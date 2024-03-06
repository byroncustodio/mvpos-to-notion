using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.SecretManager.V1;
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
using MakersManager.Models;
using MvposSDK;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly SecretsManager _secretsManager;
    private readonly Mvpos _mvpos;
    private readonly Notion _notion;

    private List<CustomSaleItem> _sales = new();
    private DateTime FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
    private DateTime ToDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddSeconds(-1);
    private int Limit { get; set; }
    private Enums.Vendor Vendor { get; set; } = Enums.Vendor.LittleSaika;
    private List<Mvpos.StoreLocation> StoreLocations { get; set; } = Enum.GetValues(typeof(Mvpos.StoreLocation)).Cast<Mvpos.StoreLocation>().ToList();

    public Function(ILogger<Function> logger, SecretManagerServiceClient secretManagerServiceClient, Mvpos mvpos, Notion notion)
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
            Vendor = (Enums.Vendor)int.Parse(query["vendor"]);
        }

        if (query.ContainsKey("locations"))
        {
            StoreLocations = Array.ConvertAll(query["locations"].ToString().Split(","), int.Parse).Cast<Mvpos.StoreLocation>().ToList();
        }

        #endregion

        await _mvpos.Users.Login("mirusakii@gmail.com", "Sakura780");

        // get sales

        var locations = await _notion.QueryDatabase<Location>(_secretsManager.GetSecret("notion-locations-id"));
        var summaryDb = await _notion.GetDatabase(_secretsManager.GetSecret("notion-summary-id"));

        foreach (var storeLocation in StoreLocations)
        {
            await _mvpos.Users.SetStoreLocation(storeLocation);

            var saleItems = await _mvpos.SaleItems.List(FromDate, ToDate);

            if (saleItems.Items is not { Count: > 0 })
            {
                continue;
            }
            
            _sales.AddRange(saleItems.Items.Select(item => new CustomSaleItem(item)));

            #region Add Summary If Not Exists

            foreach (var date in GetMonthsBetweenDates(FromDate, ToDate))
            {
                if (!_sales.Any(sale => sale.LocationId == (int)storeLocation && sale.SaleDate.ToString("Y") == date.ToString("Y")))
                {
                    continue;
                }
                    
                var location = locations.FirstOrDefault(location => location.Properties.Id.Value == (int)storeLocation);

                if (location != null)
                {
                    if (await SummaryExists(date, location)) { continue; }

                    var rowProperties = new Dictionary<string, object>
                    {
                        { "Date", NotionUtilities.CreateDateProperty(date) },
                        { "Location", NotionUtilities.CreateRelationProperty(location) }
                    };

                    await _notion.AddDatabaseRow(summaryDb.Id, rowProperties);
                }
                else
                {
                    _logger.LogError("[{Code}] - {Message}", "MISSING_LOCATION",
                        $"Location specified in request does not exist in Notion database. Create new row for location id: '{storeLocation.ToString()}'");
                }
            }

            #endregion
        }

        // filter sales by vendor

        var products = (await _notion.QueryDatabase<Product>(_secretsManager.GetSecret("notion-products-id")))
                        .Where(product => (new List<Enums.Vendor> { Vendor, Enums.Vendor.Shared })
                                            .Contains((Enums.Vendor)Enum.Parse(typeof(Enums.Vendor), product.Properties.Vendor.Select.Name)));
        var summaries = await _notion.QueryDatabase<Summary>(_secretsManager.GetSecret("notion-summary-id"));
        var salesDb = await _notion.GetDatabase(_secretsManager.GetSecret("notion-sales-id"));
        var importThreshold = 0;

        foreach (var sale in _sales)
        {
            var isEligibleForImport = false;

            var product = products.Where(product =>
            {
                if (product.Properties.SKU.RichText[0].PlainText.Split(",").Contains(sale.Sku))
                {
                    return true;
                }

                if (product.Properties.Name.Title[0].PlainText == sale.Name
                    || (product.Properties.Alias.RichText.Count > 0 
                        && product.Properties.Alias.RichText[0].PlainText == sale.Name))
                {
                    sale.NeedsReview = true;
                    return true;
                }

                return false;
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

                var location = locations.FirstOrDefault(location => location.Properties.Name.Title[0].PlainText.Replace(" ", "") == sale.LocationName);
                
                if (location != null) { sale.Location = location; }

                var summary = summaries
                    .FirstOrDefault(summary => DateTime.Parse(summary.Properties.Date.Data.Start).ToString("Y") == sale.SaleDate.ToString("Y")
                                               && summary.Properties.Location.Relations[0].Id == sale.Location.Id);
                
                if (summary != null) { sale.Summary = summary; }

                #endregion

                #region Import to Notion

                var rowProperties = new Dictionary<string, object>
                {
                    { "Sale Id", NotionUtilities.CreateTitleProperty(sale.SaleId.ToString()) },
                    { "Sale Date", NotionUtilities.CreateDateProperty(sale.SaleDate, "America/Vancouver") },
                    { "Location", NotionUtilities.CreateRelationProperty(sale.Location) },
                    { "Product", NotionUtilities.CreateRelationProperty(sale.Product) },
                    { "Payment", NotionUtilities.CreateSelectProperty(sale.Payment.Name) },
                    { "Quantity", NotionUtilities.CreateNumberProperty(sale.Quantity) },
                    { "Subtotal", NotionUtilities.CreateNumberProperty(sale.SubTotal) },
                    { "Discount", NotionUtilities.CreateNumberProperty(sale.Discount / 100) },
                    { "Total", NotionUtilities.CreateNumberProperty(sale.Total) },
                    { "Profit", NotionUtilities.CreateNumberProperty(sale.NeedsReview ? 0 : sale.Profit) },
                    { "Summary", NotionUtilities.CreateRelationProperty(sale.Summary) },
                    { "Status", NotionUtilities.CreateStatusProperty(sale.NeedsReview ? "Review" : "Done") },
                    { "SKU", NotionUtilities.CreateRichTextProperty(sale.Sku) },
                    { "Name", NotionUtilities.CreateRichTextProperty(sale.Name) },
                };

                await _notion.AddDatabaseRow(salesDb.Id, rowProperties);

                #endregion

                // update limit filter

                if (Limit <= 0) continue;
                
                importThreshold++;
                
                if (importThreshold == Limit) { break; }
            }
        }

        await context.Response.WriteAsync($"Successfully generated report. Report URL: {salesDb.Url}");
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

        if (summaryRows.Count <= 0)
        {
            return false;
        }
        
        _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SUMMARY",
            $"Skipped creating row for '{date:Y}' at '{location.Properties.Name.Title[0].PlainText}'");
        
        return true;
    }

    /*private async Task<bool> SaleExists(CustomSaleItem sale)
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

        if (saleRows.Count <= 0) return false;
        
        _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SALE",
            $"Skipped creating row for sale id '{sale.SaleId.ToString()}'");
        
        return true;
    }*/
}
