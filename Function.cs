using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ShopMakersManager;
using System;
using System.Threading.Tasks;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function : IHttpFunction
{
    private readonly ILogger _logger;
    private readonly MVPOS _mvpos;
    private readonly Notion _notion;

    public Function(ILogger<Function> logger, MVPOS mvpos, Notion notion)
    {
        _logger = logger;
        _mvpos = mvpos;
        _notion = notion;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (context.Request.Query.Count != 0)
        {
            var fromDate = new DateTime(int.Parse(context.Request.Query["year"]), int.Parse(context.Request.Query["month"]), 1);
            var toDate = fromDate.AddMonths(1).AddSeconds(-1);

            await _mvpos.Login();
            var sales = await _mvpos.GetSalesByDateRange(new() { MVPOS.StoreLocation.ParkRoyal, MVPOS.StoreLocation.Guildford }, fromDate, toDate);

            var url = await _notion.ImportSales(sales, fromDate.ToString("MMMM yyyy"));

            await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", url));
        }
        else
        {
            throw new Exception("Expected arguments not found in request. Please try again with required arguments.");
        }
    }
}
