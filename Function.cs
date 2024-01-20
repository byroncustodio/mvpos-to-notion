using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ShopMakersManager;
using System;
using System.Threading.Tasks;

namespace MakersManager;

[FunctionsStartup(typeof(Startup))]
public class Function(ILogger<Function> logger, MVPOS mvpos, Notion notion) : IHttpFunction
{
    public async Task HandleAsync(HttpContext context)
    {
        if (context.Request.Query.Count != 0)
        {
            var fromDate = new DateTime(int.Parse(context.Request.Query["year"]), int.Parse(context.Request.Query["month"]), 1);
            var toDate = fromDate.AddMonths(1).AddSeconds(-1);

            await mvpos.Login();
            var sales = await mvpos.GetSalesByDateRange([MVPOS.StoreLocation.ParkRoyal, MVPOS.StoreLocation.Guildford], fromDate, toDate);

            var url = await notion.ImportSales(sales, fromDate.ToString("MMMM yyyy"));

            await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", url));
        }
        else
        {
            await context.Response.WriteAsync("Expected parameters for request.");
        }
    }
}
