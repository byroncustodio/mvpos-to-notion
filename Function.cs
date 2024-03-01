using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.SecretManager.V1;
using MakersManager.Models.MVPOS;
using MakersManager.Models.Notion.Custom.Summary;
using MakersManager.Models.Notion.Custom.Location;
using MakersManager.Models.Notion.Custom.Product;
using MakersManager.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MakersManager.Models.Notion.Custom.Inventory;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly SecretsManager _secretsManager;
    private readonly MVPOS _mvpos;
    private readonly Notion _notion;

    private List<SaleItem> Sales = new();
    private DateTime FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
    private DateTime ToDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddSeconds(-1);
    private int Limit { get; set; } = 0;
    private MVPOS.Vendor Vendor { get; set; } = MVPOS.Vendor.LittleSaika;
    private List<MVPOS.StoreLocation> StoreLocations { get; set; } = Enum.GetValues(typeof(MVPOS.StoreLocation)).Cast<MVPOS.StoreLocation>().ToList();

    public Function(ILogger<Function> logger, SecretManagerServiceClient secretManagerServiceClient, MVPOS mvpos, Notion notion)
    {
        _logger = logger;
        _secretsManager = new SecretsManager(secretManagerServiceClient);
        _mvpos = mvpos;
        _notion = notion;
    }

    public async Task HandleAsync(HttpContext context)
    {
        #region parse query

        var query = context.Request.Query;

        if (query.ContainsKey("month") && query.ContainsKey("year"))
        {
            FromDate = new DateTime(int.Parse(query["year"]), int.Parse(query["month"]), 1);
            ToDate = FromDate.AddMonths(1).AddSeconds(-1);
        }
        else if (query.ContainsKey("month"))
        {
            FromDate = new DateTime(DateTime.Now.Year, int.Parse(query["month"]), 1);
            ToDate = FromDate.AddMonths(1).AddSeconds(-1);
        }
        else if (query.ContainsKey("year"))
        {
            FromDate = new DateTime(int.Parse(query["year"]), DateTime.Now.Month, 1);
            ToDate = FromDate.AddMonths(1).AddSeconds(-1);
        }
        else if (query.ContainsKey("from") && query.ContainsKey("to"))
        {
            FromDate = DateTime.Parse(query["from"]);
            ToDate = DateTime.Parse(query["to"]);
        }

        if (query.ContainsKey("limit"))
        {
            Limit = int.Parse(query["limit"]);
        }

        if (query.ContainsKey("vendor"))
        {
            Vendor = (MVPOS.Vendor)int.Parse(query["vendor"]);
        }

        if (query.ContainsKey("locations"))
        {
            StoreLocations = Array.ConvertAll(query["locations"].ToString().Split(","), int.Parse).Cast<MVPOS.StoreLocation>().ToList();
        }

        #endregion

        await _mvpos.Login();

        // get sales

        var locations = await _notion.QueryDatabase<Location>(_secretsManager.GetSecret("notion-locations-id"));
        var summaryDB = await _notion.GetDatabase(_secretsManager.GetSecret("notion-summary-id"));

        foreach (var storeLocation in StoreLocations)
        {
            await _mvpos.SetStoreLocation(storeLocation);

            var saleItems = await _mvpos.GetSaleItemsByDateRange(FromDate, ToDate);

            if (saleItems.Items != null && saleItems.Items.Count > 0)
            {
                Sales.AddRange(saleItems.Items);

                #region Add Summary If Not Exists

                foreach (var date in GetMonthsBetweenDates(FromDate, ToDate))
                {
                    if (Sales.Any(sale => sale.LocationId == (int)storeLocation && sale.SaleDate.ToString("Y") == date.ToString("Y")))
                    {
                        var location = locations.Where(location => location.Properties.Id.Value == (int)storeLocation).FirstOrDefault();

                        if (location != null)
                        {
                            if (await SummaryExists(date, location)) { continue; }

                            var rowProperties = new Dictionary<string, object>
                            {
                                { "Date", NotionUtilities.CreateDateProperty(date) },
                                { "Location", NotionUtilities.CreateRelationProperty(location) }
                            };

                            await _notion.AddDatabaseRow(summaryDB.Id, rowProperties);
                        }
                        else
                        {
                            _logger.LogError("[{Code}] - {Message}", "MISSING_LOCATION", string.Format("Location specified in request does not exist in Notion database. Create new row for location id: '{0}'", storeLocation.ToString()));
                        }
                    }
                }

                #endregion
            }
        }

        // filter sales by vendor

        var products = (await _notion.QueryDatabase<Product>(_secretsManager.GetSecret("notion-products-id")))
                        .Where(product => (new List<MVPOS.Vendor> { Vendor, MVPOS.Vendor.Shared })
                                            .Contains((MVPOS.Vendor)Enum.Parse(typeof(MVPOS.Vendor), product.Properties.Vendor.Select.Name)));
        var summaries = await _notion.QueryDatabase<Summary>(_secretsManager.GetSecret("notion-summary-id"));
        var inventories = await _notion.QueryDatabase<Inventory>(_secretsManager.GetSecret("notion-inventory-id"));
        var salesDB = await _notion.GetDatabase(_secretsManager.GetSecret("notion-sales-id"));
        var importThreshold = 0;

        foreach (var sale in Sales)
        {
            var isEligibleForImport = false;

            var product = products.Where(product =>
            {
                if (product.Properties.SKU.RichText[0].PlainText.Split(",").Contains(sale.Sku))
                {
                    return true;
                }
                else if (product.Properties.Name.Title[0].PlainText == sale.Name
                            || (product.Properties.Alias.RichText.Count > 0 
                                && product.Properties.Alias.RichText[0].PlainText == sale.Name))
                {
                    sale.NeedsReview = true;
                    return true;
                }
                else { return false; }
            }).FirstOrDefault();

            if (product != null)
            {
                sale.Product = product;
                isEligibleForImport = true;
            }
            else if (string.IsNullOrEmpty(sale.Sku) || string.IsNullOrEmpty(sale.Name))
            {
                sale.NeedsReview = true;
                isEligibleForImport = true;
            }

            if (isEligibleForImport)
            {
                #region Set Relation Properties

                var location = locations.Where(location => location.Properties.Name.Title[0].PlainText.Replace(" ", "") == sale.LocationName)
                                        .FirstOrDefault();
                if (location != null) { sale.Location = location; }

                var summary = summaries.Where(summary => DateTime.Parse(summary.Properties.Date.Data.Start).ToString("Y") == sale.SaleDate.ToString("Y")
                                                            && summary.Properties.Location.Relations[0].Id == sale.Location.Id)
                                       .FirstOrDefault();
                if (summary != null) { sale.Summary = summary; }

                if (sale.Product != null)
                {
                    var inventory = inventories.Where(inventory => inventory.Properties.Product.Relations[0].Id == sale.Product.Id
                                                                    && inventory.Properties.Location.Relations[0].Id == sale.Location.Id)
                                               .FirstOrDefault();
                    if (inventory != null) { sale.Inventory = inventory; }
                    else { sale.NeedsReview = true; }
                }

                #endregion

                #region Import to Notion

                var rowProperties = new Dictionary<string, object>
                {
                    { "Sale Id", NotionUtilities.CreateTitleProperty(sale.SaleId.ToString()) },
                    { "Sale Date", NotionUtilities.CreateDateProperty(sale.SaleDate, "America/Vancouver") },
                    { "Location", NotionUtilities.CreateRelationProperty(sale.Location) },
                    { "Product", NotionUtilities.CreateRelationProperty(sale.Product) },
                    { "Payment", NotionUtilities.CreateSelectProperty(sale.PaymentName) },
                    { "Quantity", NotionUtilities.CreateNumberProperty(sale.Quantity) },
                    { "Subtotal", NotionUtilities.CreateNumberProperty(sale.SubTotal) },
                    { "Discount", NotionUtilities.CreateNumberProperty(sale.Discount / 100) },
                    { "Total", NotionUtilities.CreateNumberProperty(sale.Total) },
                    { "Profit", NotionUtilities.CreateNumberProperty(sale.NeedsReview ? 0 : sale.Profit) },
                    { "Summary", NotionUtilities.CreateRelationProperty(sale.Summary) },
                    { "Status", NotionUtilities.CreateStatusProperty(sale.NeedsReview ? "Review" : "Done") },
                    { "SKU", NotionUtilities.CreateRichTextProperty(sale.Sku) },
                    { "Name", NotionUtilities.CreateRichTextProperty(sale.Name) },
                    { "Inventory", NotionUtilities.CreateRelationProperty(sale.Inventory) },
                };

                await _notion.AddDatabaseRow(salesDB.Id, rowProperties);

                #endregion

                // update limit filter

                if (Limit > 0)
                {
                    importThreshold++;
                    if (importThreshold == Limit) { break; }
                }
            }
        }

        await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", ""));
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

    private async Task<bool> SummaryExists(DateTime date, Location location)
    {
        var and = new List<object>
        {
            NotionUtilities.CreateDateFilter("Date", NotionUtilities.FilterCondition.Equals, date.ToString("yyyy-MM-dd")),
            NotionUtilities.CreateRelationFilter("Location", NotionUtilities.FilterCondition.Contains, location.Id)
        };

        var summaryRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-summary-id"), new { and });

        if (summaryRows.Count > 0)
        {
            _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SUMMARY", string.Format("Skipped creating row for '{0}' at '{1}'", date.ToString("Y"), location.Properties.Name.Title[0].PlainText));
            return true;
        }

        return false;
    }

    //private async Task<bool> SaleExists(SaleItem sale)
    //{
    //    var and = new List<object>
    //    {
    //        NotionUtilities.CreateTitleFilter("Sale Id", NotionUtilities.FilterCondition.Equals, sale.SaleId.ToString()),
    //        NotionUtilities.CreateNumberFilter("Quantity", NotionUtilities.FilterCondition.Equals, sale.Quantity)
    //    };

    //    if (sale.Location != null)
    //    { 
    //        and.Add(NotionUtilities.CreateRelationFilter("Location", NotionUtilities.FilterCondition.Contains, sale.Location.Id)); 
    //    }

    //    if (sale.Product != null)
    //    {
    //        and.Add(NotionUtilities.CreateRelationFilter("Product", NotionUtilities.FilterCondition.Contains, sale.Product.Id));
    //    }

    //    var saleRows = await _notion.QueryDatabase(_secretsManager.GetSecret("notion-sales-id"), new { and });

    //    if (saleRows.Count > 0)
    //    {
    //        _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SALE", string.Format("Skipped creating row for sale id '{0}'", sale.SaleId.ToString()));
    //        return true;
    //    }

    //    return false;
    //}
}
