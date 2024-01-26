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

    private List<SaleItem> sales = new();

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

        var fromDate = (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).AddMonths(-1);
        var limit = 0;
        var vendor = MVPOS.Vendor.LittleSaika;
        var locations = new List<MVPOS.StoreLocation>() { MVPOS.StoreLocation.ParkRoyal, MVPOS.StoreLocation.Guildford, MVPOS.StoreLocation.Victoria };

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

        await GetSales(locations, fromDate, fromDate.AddMonths(1).AddSeconds(-1));

        ApplyFilterAndSort(limit, vendor);

        await SetRelations();

        var url = await ImportSales();

        await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", url));
    }

    private async Task GetSales(List<MVPOS.StoreLocation> locations, DateTime from, DateTime to)
    {
        foreach (var location in locations)
        {
            await _mvpos.SetStoreLocation(location);

            var saleItems = await _mvpos.GetSaleItemsByDateRange(from, to);

            if (saleItems.Items != null)
            {
                sales.AddRange(saleItems.Items);
            }
        }
    }

    private void ApplyFilterAndSort(int limit, MVPOS.Vendor vendor)
    {
        string skuSecretId;

        switch (vendor)
        {
            case MVPOS.Vendor.LittleSaika:
            default:
                skuSecretId = "mvpos-skus-ls";
                break;
            case MVPOS.Vendor.SukitaStudio:
                skuSecretId = "mvpos-skus-ss";
                break;
        }

        var skus = _secretsManager.GetSecret(skuSecretId, "1").Split("\n").ToList();
        var skuCode = _secretsManager.GetSecret("mvpos-sku-code", "1").ToString();

        sales = sales.Where(x => skus.Contains(x.Sku) || x.Sku == null || !x.Sku.Contains(skuCode)).ToList();

        if (limit > 0)
        {
            sales = sales.Take(limit).ToList();
        }

        sales = sales.OrderBy(x => x.SaleDate).ToList();
    }

    private async Task SetRelations()
    {
        var locations = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-locations-id", "1"));
        var products = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-products-id", "1"));
        var analytics = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-analytics-id", "1"));

        foreach (var sale in sales)
        {
            foreach (JObject locationObj in locations.Cast<JObject>())
            {
                Location location = locationObj.ToObject<Location>();

                if (location.Properties.Name.Title[0].PlainText.Replace(" ", "") == sale.LocationName)
                {
                    sale.Location = location;
                    break;
                }
            }

            foreach (JObject productObj in products.Cast<JObject>())
            {
                Product product = productObj.ToObject<Product>();

                if (product.Properties.SKU.RichText[0].PlainText == sale.Sku)
                {
                    sale.Product = product;
                    break;
                }
            }

            foreach (JObject analyticsObj in analytics.Cast<JObject>())
            {
                Analytic analytic = analyticsObj.ToObject<Analytic>();

                if (DateTime.Parse(analytic.Properties.Month.Title[0].PlainText).ToString("MM/yy") == sale.SaleDate.ToString("MM/yy")
                    && analytic.Properties.Location.Relations[0].Id == sale.Location.Id)
                {
                    sale.Analytic = analytic;
                    break;
                }
            }
        }
    }

    private async Task<Database> ImportSales()
    {
        var database = await _notion.GetDatabase(_secretsManager.GetSecret("notion-sales-id", "1"));

        foreach (var sale in sales)
        {
            var existingSales = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-sales-id", "1"), new
            {
                property = "Sale Id",
                title = new { equals = sale.SaleId.ToString() }
            });

            if (existingSales.Count > 0) 
            {
                _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SALE", string.Format("Skipped creating row for sale id '{0}'", sale.SaleId.ToString()));
                continue; 
            }

            var rowProperties = new Dictionary<string, object>
            {
                { "Sale Id", NotionUtilities.CreateTitleProperty(sale.SaleId.ToString()) },
                { "Sale Date", NotionUtilities.CreateDateProperty(sale.SaleDate, "America/Vancouver") },
                { "Location", NotionUtilities.CreateRelationProperty(sale.LocationRelation) },
                //{ "SKU", NotionUtilities.CreateRichTextProperty(sale.Sku) },
                { "Product", NotionUtilities.CreateRelationProperty(sale.ProductRelation) },
                { "Payment", NotionUtilities.CreateSelectProperty(sale.PaymentName) },
                { "Quantity", NotionUtilities.CreateNumberProperty(sale.Quantity) },
                { "Subtotal", NotionUtilities.CreateNumberProperty(sale.SubTotal) },
                { "Discount", NotionUtilities.CreateNumberProperty(sale.Discount / 100) },
                { "Total", NotionUtilities.CreateNumberProperty(sale.Total) },
                { "Profit", NotionUtilities.CreateNumberProperty(sale.Profit) },
                { "Analytic", NotionUtilities.CreateRelationProperty(sale.AnalyticRelation) },
            };

            await _notion.AddDatabaseRow(database.Id, rowProperties);
        }

        return database;
    }
}
