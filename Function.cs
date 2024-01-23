using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        var sales = await _mvpos.GetSalesByDateRange(locations, fromDate, fromDate.AddMonths(1).AddSeconds(-1));
        sales = _mvpos.ApplySaleFilters(sales, limit, vendor);

        sales = await _notion.SetSaleItemRelations(sales);
        var url = await _notion.ImportSales(sales, fromDate.ToString("MMMM yyyy"));

        await context.Response.WriteAsync(string.Format("Successfully generated report. Report URL: {0}", url));
    }
}
