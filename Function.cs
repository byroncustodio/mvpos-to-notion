using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.SecretManager.V1;
using mvpos.Models.Mvpos;
using mvpos.Models.Notion;
using mvpos.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using mvpos.Models;
using MvposSDK;
using Newtonsoft.Json;
using NotionSDK;
using NotionSDK.Extensions;
using NotionSDK.Models.Block;
using NotionSDK.Models.Property;

namespace mvpos;

[FunctionsStartup(typeof(Startup))]
public class Function(
    ILogger<Function> logger,
    SecretManagerServiceClient secretManagerServiceClient,
    Mvpos mvpos,
    Notion notion)
    : IHttpFunction
{
    private readonly ILogger _logger = logger;
    private readonly SecretsManager _secretsManager = new(secretManagerServiceClient);

    private readonly List<CustomSaleItem> _sales = new();
    private List<Mvpos.StoreLocation> StoreLocations { get; set; } = Enum.GetValues(typeof(Mvpos.StoreLocation)).Cast<Mvpos.StoreLocation>().ToList();

    public async Task HandleAsync(HttpContext context)
    {
        if (context.Request.Method != HttpMethods.Post)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var body = JsonConvert.DeserializeObject<RequestBody>(await reader.ReadToEndAsync()) ?? throw new JsonException("Deserialized JSON resulted in null value.");

        switch (body.Range.Type)
        {
            case "PastWeek":
                body.Range.From = DateTime.Now.AddDays(-7).Date;
                body.Range.To = DateTime.Now.Date.AddSeconds(-1);
                break;
        }
        
        if (!string.IsNullOrEmpty(body.Locations))
        {
            StoreLocations = Array.ConvertAll(body.Locations.Split(","), int.Parse).Cast<Mvpos.StoreLocation>().ToList();
        }

        try
        {
            await mvpos.Users.Login(body.Email ?? _secretsManager.GetSecretFromString("mvpos-user"),
                body.Password ?? _secretsManager.GetSecretFromString("mvpos-password"));
            notion.Configure(_secretsManager.GetSecretFromString("notion-base-url"),
                _secretsManager.GetSecretFromString("notion-token"));

            if (body.UploadType == "Basic")
            {
                await mvpos.Users.SetStoreLocation(Mvpos.StoreLocation.Victoria);

                var saleItems = (await mvpos.SaleItems.List(body.Range.From, body.Range.To)).Items;

                if (saleItems is not { Count: > 0 })
                {
                    await context.Response.WriteAsync($"No sales found.");
                }

                var salesMetadata = await notion.GetDatabaseMetadata(body.NotionPageId);

                foreach (var sale in body.Limit <= 0 ? saleItems : saleItems.Take(body.Limit))
                {
                    var rowProperties = new PropertyBuilder();
                    rowProperties.Add("Sale Id", new Title(sale.SaleId.ToString()));
                    rowProperties.Add("Sale Date", new Date(sale.SaleDate.ToString("s"), "America/Vancouver"));
                    rowProperties.Add("Payment", new Select(sale.Payment.Name == "" ? "N/A" : sale.Payment.Name));
                    rowProperties.Add("Quantity", new Number(sale.Quantity));
                    rowProperties.Add("Subtotal", new Number(sale.SubTotal));
                    rowProperties.Add("Discount", new Number(sale.Discount / 100));
                    rowProperties.Add("Total", new Number(sale.Total));
                    rowProperties.Add("SKU", new RichText(sale.Sku));
                    rowProperties.Add("Name", new RichText(sale.Name));

                    if (body.Debug != 1)
                    {
                        await notion.AddDatabaseRow(salesMetadata.Id, rowProperties.Build());
                    }
                }

                await context.Response.WriteAsync($"Successfully generated report. Report URL: {salesMetadata.Url}");
            }
            else
            {
                // get sales

                var locationPages = (await notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-locations-id")))
                    .Select(result => new Location(result))
                    .ToList();

                var summaryDatabase =
                    await notion.GetDatabaseMetadata(_secretsManager.GetSecretFromString("notion-summary-id"));

                var summaryPages = (await notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-summary-id")))
                    .Select(result => new Summary(result))
                    .ToList();

                foreach (var storeLocation in StoreLocations)
                {
                    await mvpos.Users.SetStoreLocation(storeLocation);

                    var saleItems = (await mvpos.SaleItems.List(body.Range.From, body.Range.To)).Items.Where(item => item.LocationId == (int)storeLocation).ToList();

                    if (saleItems is not { Count: > 0 }) { continue; }

                    _sales.AddRange(saleItems.Select(item => new CustomSaleItem(item)));

                    #region Add Summary If Not Exists

                    var location = locationPages.FirstOrDefault(location => location.Properties.Id.Value == (int)storeLocation);

                    if (location != null)
                    {
                        foreach (var date in GetMonthsBetweenDates(body.Range.From, body.Range.To).Where(date =>
                                     _sales.Any(sale =>
                                         sale.LocationId == (int)storeLocation &&
                                         sale.SaleDate.ToString("Y") == date.ToString("Y")) &&
                                     !summaryPages.Any(summary =>
                                         summary.Properties.Date.Data?.Start == date.ToString("yyyy-MM-dd") &&
                                         summary.Properties.Location.Data[0].Id == location.Id)))
                        {
                            var rowProperties = new PropertyBuilder();
                            rowProperties.Add("Date", new Date(new DateData { Start = date.ToString("yyyy-MM-dd") }));
                            rowProperties.Add("Location", new Relation([new PageReference { Id = location.Id }]));

                            if (body.Debug != 1)
                            {
                                summaryPages.Add(new Summary(await notion.AddDatabaseRow(summaryDatabase.Id ?? throw new InvalidOperationException(), rowProperties.Build())));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("[{Code}] - {Message}", "MISSING_LOCATION",
                            $"Location specified in request does not exist in Notion database. Create new row for location id: '{storeLocation.ToString()}'");
                    }

                    #endregion
                }

                // filter sales by vendor

                var products = (await notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-products-id")))
                    .Select(result => new Product(result))
                    .ToList();

                var inventories = (await notion.QueryDatabase(_secretsManager.GetSecretFromString("notion-inventory-id")))
                    .Select(result => new Inventory(result))
                    .ToList();

                var salesMetadata = await notion.GetDatabaseMetadata(_secretsManager.GetSecretFromString("notion-sales-id"));

                foreach (var sale in body.Limit <= 0 ? _sales.Where(sale => ValidateSale(sale, products)) : _sales.Where(sale => ValidateSale(sale, products)).Take(body.Limit))
                {
                    PopulateRelations(sale, locationPages, inventories, summaryPages);

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
                    rowProperties.Add("Profit", new Number(sale.NeedsReview ? 0 : sale.GetProfit()));
                    rowProperties.Add("Summary", new Relation(sale.Summary));
                    rowProperties.Add("Status", new Status(sale.NeedsReview ? "Review" : "Done"));
                    rowProperties.Add("SKU", new RichText(sale.Sku));
                    rowProperties.Add("Name", new RichText(sale.Name));
                    rowProperties.Add("Inventory", new Relation(sale.Inventory));

                    if (body.Debug != 1)
                    {
                        await notion.AddDatabaseRow(salesMetadata.Id, rowProperties.Build());
                    }
                }

                await context.Response.WriteAsync($"Successfully generated report. Report URL: {salesMetadata.Url}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[{Code}] - {Message} - {Exception}", "THROWN_EXCEPTION", $"{ex.Message}", $"{ex}");
            await context.Response.WriteAsync("There was a problem running the application.");
        }
    }

    private static List<DateTime> GetMonthsBetweenDates(DateTime start, DateTime end)
    {
        start = new DateTime(start.Year, start.Month, 1);
        end = new DateTime(end.Year, end.Month, end.AddMonths(1).AddSeconds(-1).Day);
        var dates = new List<DateTime>();

        while (start <= end)
        {
            dates.Add(start);
            start = start.AddMonths(1);
        }

        return dates;
    }

    private static bool ValidateSale(CustomSaleItem sale, IEnumerable<Product> products)
    {
        var product = products.Where(product =>
        {
            if (product.Properties.Sku.Data.Count != 0 && product.Properties.Sku.Data[0].PlainText != null && product.Properties.Sku.Data[0].PlainText.Split(",").Contains(sale.Sku))
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
            return true;
        }

        if (!string.IsNullOrEmpty(sale.Sku) && !string.IsNullOrEmpty(sale.Name))
        {
            return false;
        }

        sale.NeedsReview = true;
        return true;
    }

    private static void PopulateRelations(CustomSaleItem sale, IEnumerable<Location> locations, IEnumerable<Inventory> inventories, IEnumerable<Summary> summaries)
    {
        var location = locations.FirstOrDefault(location => location.Properties.Name.Data[0].PlainText == sale.LocationName);

        if (location != null) { sale.Location = location; }

        Inventory inventory = null;

        if (sale.Product != null && sale.Location != null)
        {
            inventory = inventories.FirstOrDefault(inv => inv.Properties.Product.Data.Count > 0 && inv.Properties.Product.Data[0].Id == sale.Product.Id &&
                                                          inv.Properties.Location.Data.Count > 0 && inv.Properties.Location.Data[0].Id == sale.Location.Id);
        }
        else if (sale.Product == null && sale.Location != null)
        {
            inventory = inventories.FirstOrDefault(inv => inv.Properties.Location.Data.Count > 0 && inv.Properties.Location.Data[0].Id == sale.Location.Id);
        }

        if (inventory != null)
        {
            sale.Inventory = inventory;
        }

        var summary = summaries
            .FirstOrDefault(summary => DateTime.Parse(summary.Properties.Date.Data.Start).ToString("Y") == sale.SaleDate.ToString("Y")
                                       && summary.Properties.Location.Data[0].Id == sale.Location.Id);

        if (summary != null) { sale.Summary = summary; }
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
