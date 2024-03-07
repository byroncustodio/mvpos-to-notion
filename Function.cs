using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.SecretManager.V1;
using MakersManager.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MakersManager.Models.Mvpos;
using MakersManager.Models.Notion;
using MvposSDK;
using NotionSDK;
using NotionSDK.Extensions;
using NotionSDK.Models.Block;
using NotionSDK.Models.Property;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly SecretsManager _secretsManager;
    private readonly Mvpos _mvpos;
    private readonly Notion _notion;

    private readonly List<CustomSaleItem> _sales = new();
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

        try
        {
            await _mvpos.Users.Login(_secretsManager.GetSecretFromString("mvpos-user"), _secretsManager.GetSecretFromString("mvpos-password"));
            _notion.Configure(_secretsManager.GetSecretFromString("notion-base-url"), _secretsManager.GetSecretFromString("notion-token"));
            
            // get sales

            var locations = (await _notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-locations-id"))).Results
                .Select(result => new Location(result))
                .ToList();
            
            var summaryMetadata = await _notion.GetDatabaseMetadata(_secretsManager.GetSecretFromString("notion-summary-id"));

            foreach (var storeLocation in StoreLocations)
            {
                await _mvpos.Users.SetStoreLocation(storeLocation);

                var saleItems = (await _mvpos.SaleItems.List(FromDate, ToDate)).Items;

                if (saleItems is not { Count: > 0 }) { continue; }
                
                _sales.AddRange(saleItems.Select(item => new CustomSaleItem(item)));

                #region Add Summary If Not Exists

                foreach (var date in GetMonthsBetweenDates(FromDate, ToDate).Where(date => _sales.Any(sale => sale.LocationId == (int)storeLocation && sale.SaleDate.ToString("Y") == date.ToString("Y"))))
                {
                    var location = locations.FirstOrDefault(location => location.Properties.Id.Value == (int)storeLocation);

                    if (location != null)
                    {
                        if (await SummaryExists(date, location)) { continue; }

                        var rowProperties = new PropertyBuilder();
                        rowProperties.Add("Date", new Date(new DateData { Start = date.ToString("yyyy-MM-dd") }));
                        rowProperties.Add("Location", new Relation(new List<PageReference> { new() { Id = location.Id } }));

                        await _notion.AddDatabaseRow(summaryMetadata.Id, rowProperties.Build());
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

            var products = (await _notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-products-id"))).Results
                .Select(result => new Product(result))
                .Where(product => 
                    new List<Enums.Vendor> { Vendor, Enums.Vendor.Shared }
                        .Contains((Enums.Vendor)Enum.Parse(typeof(Enums.Vendor), product.Properties.Vendor.Data.Name)))
                .ToList();
                
            var summaries = (await _notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-summary-id"))).Results
                .Select(result => new Summary(result))
                .ToList();
            var salesMetadata = await _notion.GetDatabaseMetadata(_secretsManager.GetSecretFromString("notion-sales-id"));
            var importThreshold = 0;

            foreach (var sale in _sales)
            {
                var isEligibleForImport = false;

                var product = products.Where(product =>
                {
                    if (product.Properties.Sku.Data[0].PlainText.Split(",").Contains(sale.Sku))
                    {
                        return true;
                    }

                    if (product.Properties.Name.Data[0].PlainText != sale.Name
                        && (product.Properties.Alias.Data.Count <= 0
                            || product.Properties.Alias.Data[0].PlainText != sale.Name))
                    {
                        return false;
                    }
                    
                    sale.NeedsReview = true;
                    return true;
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

                if (!isEligibleForImport) { continue; }

                #region Set Relation Properties

                var location = locations.FirstOrDefault(location => location.Properties.Name.Data[0].PlainText == sale.LocationName);
                    
                if (location != null) { sale.Location = location; }

                var summary = summaries
                    .FirstOrDefault(summary => DateTime.Parse(summary.Properties.Date.Data.Start).ToString("Y") == sale.SaleDate.ToString("Y")
                                               && summary.Properties.Location.Data[0].Id == sale.Location.Id);
                    
                if (summary != null) { sale.Summary = summary; }

                #endregion

                #region Import to Notion

                var rowProperties = new PropertyBuilder();
                rowProperties.Add("Sale Id", new Title(sale.SaleId.ToString()));
                rowProperties.Add("Sale Date", new Date(sale.SaleDate.ToString("s"), "America/Vancouver"));
                rowProperties.Add("Location", new Relation(sale.Location));
                rowProperties.Add("Product", new Relation(sale.Product));
                rowProperties.Add("Payment", new Select(sale.Payment.Name));
                rowProperties.Add("Quantity", new Number(sale.Quantity));
                rowProperties.Add("Subtotal", new Number(sale.SubTotal));
                rowProperties.Add("Discount", new Number(sale.Discount / 100));
                rowProperties.Add("Total", new Number(sale.Total));
                rowProperties.Add("Profit", new Number(sale.NeedsReview ? 0 : sale.Profit));
                rowProperties.Add("Summary", new Relation(sale.Summary));
                rowProperties.Add("Status", new Status(sale.NeedsReview ? "Review" : "Done"));
                rowProperties.Add("SKU", new RichText(sale.Sku));
                rowProperties.Add("Name", new RichText(sale.Name));

                await _notion.AddDatabaseRow(salesMetadata.Id, rowProperties.Build());

                #endregion

                // update limit filter

                if (Limit <= 0) continue;
                    
                importThreshold++;
                    
                if (importThreshold == Limit) { break; }
            }

            await context.Response.WriteAsync($"Successfully generated report. Report URL: {salesMetadata.Url}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{error}", ex.Message);
        }
    }

    private IEnumerable<DateTime> GetMonthsBetweenDates(DateTime start, DateTime end)
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
        var filter = new QueryFilter();
        filter.Add<Date>("Date", Comparator.Equals, date.ToString("yyyy-MM-dd"));
        filter.Add<Relation>("Location", Comparator.Contains, location.Id);

        var summaryRows = (await _notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-summary-id"), filter.Build(Operand.And))).Results;

        if (summaryRows.Count <= 0)
        {
            return false;
        }
        
        _logger.LogInformation("[{Code}] - {Message}", "DUPLICATE_SUMMARY",
            $"Skipped creating row for '{date:Y}' at '{location.Properties.Name.Data[0].PlainText}'");
        
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
