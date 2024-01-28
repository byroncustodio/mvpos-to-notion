using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.SecretManager.V1;
using MakersManager.Models.MVPOS;
using MakersManager.Models.Notion;
using MakersManager.Models.Notion.Database;
using MakersManager.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly SecretsManager _secretsManager;
    private readonly MVPOS _mvpos;
    private readonly Notion _notion;
    private List<SaleItem> _sales = new();
    private List<Tuple<DateTime, MVPOS.StoreLocation>> _analysisRowsToCreate = new();

    public Function(ILogger<Function> logger, SecretManagerServiceClient secretManagerServiceClient, MVPOS mvpos, Notion notion)
    {
        _logger = logger;
        _secretsManager = new SecretsManager(secretManagerServiceClient);
        _mvpos = mvpos;
        _notion = notion;
    }

    public async Task HandleAsync(HttpContext context)
    {
        // default query values

        var fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
        var toDate = fromDate.AddMonths(1).AddSeconds(-1);
        var limit = 0;
        var vendor = MVPOS.Vendor.LittleSaika;
        List<MVPOS.StoreLocation> locations = Enum.GetValues(typeof(MVPOS.StoreLocation)).Cast<MVPOS.StoreLocation>().ToList();

        #region parse query

        var query = context.Request.Query;

        if (query.ContainsKey("month") && query.ContainsKey("year"))
        {
            fromDate = new DateTime(int.Parse(query["year"]), int.Parse(query["month"]), 1);
        }
        else if (query.ContainsKey("month"))
        {
            fromDate = new DateTime(DateTime.Now.Year, int.Parse(query["month"]), 1);
        }
        else if (query.ContainsKey("year"))
        {
            fromDate = new DateTime(int.Parse(query["year"]), DateTime.Now.Month, 1);
        }
        else if (query.ContainsKey("from") && query.ContainsKey("to"))
        {
            fromDate = DateTime.Parse(query["from"]);
            toDate = DateTime.Parse(query["to"]);
        }

        if (query.ContainsKey("limit"))
        {
            limit = int.Parse(query["limit"]);
        }

        if (query.ContainsKey("vendor"))
        {
            vendor = (MVPOS.Vendor)int.Parse(query["vendor"]);
        }

        if (query.ContainsKey("locations"))
        {
            locations = Array.ConvertAll(query["locations"].ToString().Split(","), int.Parse).Cast<MVPOS.StoreLocation>().ToList();
        }

        #endregion

        await _mvpos.Login();

        await GetSales(locations, fromDate, toDate);

        await ApplyFilterAndSort(limit, vendor);

        await ImportAnalytics();

        await SetRelations();

        var database = await ImportSales();

        await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", database.Url));
    }

    private async Task GetSales(List<MVPOS.StoreLocation> locations, DateTime from, DateTime to)
    {
        foreach (var location in locations)
        {
            await _mvpos.SetStoreLocation(location);

            var saleItems = await _mvpos.GetSaleItemsByDateRange(from, to);

            if (saleItems.Items != null)
            {
                _sales.AddRange(saleItems.Items);

                foreach (var date in GetMonthsBetweenDates(from, to))
                {
                    if (saleItems.Items.Any(sale => sale.LocationId == (int)location && sale.SaleDate.ToString("Y") == date.ToString("Y")))
                    {
                        _analysisRowsToCreate.Add(Tuple.Create(date, location));
                    }
                }
            }
        }
    }

    private async Task ApplyFilterAndSort(int limit, MVPOS.Vendor vendor)
    {
        var productRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-products-id"));
        var vendorsToFilter = new List<MVPOS.Vendor> { vendor, MVPOS.Vendor.Shared };

        var products = productRows
            .Cast<JObject>()
            .Where(x => vendorsToFilter.Contains((MVPOS.Vendor)Enum.Parse(typeof(MVPOS.Vendor), x.ToObject<Product>().Properties.Vendor.Select.Name)))
            .Select(x => x.ToObject<Product>())
            .ToList();

        var skuCode = _secretsManager.GetSecret("mvpos-sku-code");
        List<string> skus = new();

        foreach (var product in products) 
        {
            skus.AddRange(product.Properties.SKU.RichText[0].PlainText.Split(","));
        }

        _sales = _sales.Where(sale => skus.Contains(sale.Sku) || sale.Sku == null || !sale.Sku.Contains(skuCode)).ToList();

        if (limit > 0)
        {
            _sales = _sales.Take(limit).ToList();
        }

        _sales = _sales.OrderBy(sale => sale.SaleDate).ToList();
    }

    private async Task ImportAnalytics()
    {
        var locationRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-locations-id"));
        var analyticsDB = await _notion.GetDatabase(_secretsManager.GetSecret("notion-analytics-id"));

        foreach (var analysisRow in _analysisRowsToCreate.OrderBy(row => row.Item1).ThenBy(row => row.Item2))
        {
            var activeDate = analysisRow.Item1;
            var activeLocation = analysisRow.Item2;
            Location locationData = null;

            foreach (JObject locationRow in locationRows.Cast<JObject>())
            {
                Location location = locationRow.ToObject<Location>();

                if (activeLocation == (MVPOS.StoreLocation)Enum.Parse(typeof(MVPOS.StoreLocation), location.Properties.Name.Title[0].PlainText.Replace(" ", "")))
                {
                    locationData = location;
                    break;
                }
            }

            if (locationData != null)
            {
                if (await AnalysisExists(activeDate, locationData)) { continue; }

                var rowProperties = new Dictionary<string, object>
                {
                    { "Date", NotionUtilities.CreateDateProperty(activeDate) },
                    { "Location", NotionUtilities.CreateRelationProperty(locationData) },
                    { "Status", NotionUtilities.CreateStatusProperty(activeDate < DateTime.Now ? "Done" : "In Progress") }
                };

                await _notion.AddDatabaseRow(analyticsDB.Id, rowProperties);
            }
        }
    }

    private async Task SetRelations()
    {
        var locationRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-locations-id"));
        var productRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-products-id"));
        var analysisRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-analytics-id"));

        foreach (var sale in _sales)
        {
            foreach (JObject row in locationRows.Cast<JObject>())
            {
                Location location = row.ToObject<Location>();

                if (location.Properties.Name.Title[0].PlainText.Replace(" ", "") == sale.LocationName)
                {
                    sale.Location = location;
                    break;
                }
            }

            foreach (JObject row in productRows.Cast<JObject>())
            {
                Product product = row.ToObject<Product>();

                if (product.Properties.SKU.RichText[0].PlainText.Split(",").Contains(sale.Sku))
                {
                    sale.Product = product;
                    break;
                }
            }

            foreach (JObject row in analysisRows.Cast<JObject>())
            {
                Analysis analysis = row.ToObject<Analysis>();

                if (DateTime.Parse(analysis.Properties.Date.Data.Start).ToString("Y") == sale.SaleDate.ToString("Y")
                    && analysis.Properties.Location.Relations[0].Id == sale.Location.Id)
                {
                    sale.Analysis = analysis;
                    break;
                }
            }
        }
    }

    private async Task<Database> ImportSales()
    {
        var salesDB = await _notion.GetDatabase(_secretsManager.GetSecret("notion-sales-id"));

        foreach (var sale in _sales)
        {
            if (await SaleExists(sale)) { continue; }

            var rowProperties = new Dictionary<string, object>
            {
                { "Sale Id", NotionUtilities.CreateTitleProperty(sale.SaleId.ToString()) },
                { "Sale Date", NotionUtilities.CreateDateProperty(sale.SaleDate, "America/Vancouver") },
                { "Location", NotionUtilities.CreateRelationProperty(sale.Location) },
                //{ "SKU", NotionUtilities.CreateRichTextProperty(sale.Sku) },
                { "Product", NotionUtilities.CreateRelationProperty(sale.Product) },
                { "Payment", NotionUtilities.CreateSelectProperty(sale.PaymentName) },
                { "Quantity", NotionUtilities.CreateNumberProperty(sale.Quantity) },
                { "Subtotal", NotionUtilities.CreateNumberProperty(sale.SubTotal) },
                { "Discount", NotionUtilities.CreateNumberProperty(sale.Discount / 100) },
                { "Total", NotionUtilities.CreateNumberProperty(sale.Total) },
                { "Profit", NotionUtilities.CreateNumberProperty(sale.Profit) },
                { "Analytic", NotionUtilities.CreateRelationProperty(sale.Analysis) },
            };

            await _notion.AddDatabaseRow(salesDB.Id, rowProperties);
        }

        return salesDB;
    }

    private List<DateTime> GetMonthsBetweenDates(DateTime start, DateTime end)
    {
        var dates = new List<DateTime>();

        while (start <= end)
        {
            dates.Add(start);
            start = start.AddMonths(1);
        }

        return dates;
    }

    private async Task<bool> AnalysisExists(DateTime date, Location location)
    {
        var and = new List<object>
        {
            NotionUtilities.CreateDateFilter("Date", NotionUtilities.FilterCondition.Equals, date.ToString("yyyy-MM-dd")),
            NotionUtilities.CreateRelationFilter("Location", NotionUtilities.FilterCondition.Contains, location.Id)
        };

        var analysisRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-analytics-id"), new { and });

        if (analysisRows.Count > 0)
        {
            _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_ANALYSIS", string.Format("Skipped creating row for '{0}' at '{1}'", date.ToString("Y"), location.Properties.Name.Title[0].PlainText));
            return true;
        }

        return false;
    }

    private async Task<bool> SaleExists(SaleItem sale)
    {
        var and = new List<object>
        {
            NotionUtilities.CreateTitleFilter("Sale Id", NotionUtilities.FilterCondition.Equals, sale.SaleId.ToString()),
            NotionUtilities.CreateNumberFilter("Quantity", NotionUtilities.FilterCondition.Equals, sale.Quantity)
        };

        if (sale.Location != null)
        { 
            and.Add(NotionUtilities.CreateRelationFilter("Location", NotionUtilities.FilterCondition.Contains, sale.Location.Id)); 
        }

        if (sale.Product != null)
        {
            and.Add(NotionUtilities.CreateRelationFilter("Product", NotionUtilities.FilterCondition.Contains, sale.Product.Id));
        }

        var saleRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-sales-id"), new { and });

        if (saleRows.Count > 0)
        {
            _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SALE", string.Format("Skipped creating row for sale id '{0}'", sale.SaleId.ToString()));
            return true;
        }

        return false;
    }
}
