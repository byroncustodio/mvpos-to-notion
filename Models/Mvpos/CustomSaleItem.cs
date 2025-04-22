using System.Collections.Generic;
using System.Linq;
using MakersManager.Models.Notion;
using MvposSDK.Models;
using Location = MakersManager.Models.Notion.Location;

namespace MakersManager.Models.Mvpos;

public class CustomSaleItem(SaleItem saleItem) : SaleItem(saleItem)
{
    public bool NeedsReview { get; set; }

    public Product Product { get; set; }

    public Location Location { get; set; }

    public Summary Summary { get; set; }

    public Inventory Inventory { get; set; }

    public decimal GetProfit(List<string> vendors)
    {
        var numOfVendors = vendors.Count;

        if (Product == null)
        {
            if (Price == 25m) // Assume this is a mystery box sale
            {
                return Total / numOfVendors;
            }

            // Cant determine which vendor, sale is split evenly
            return Total / numOfVendors;
        }

        if (Product.Properties.Vendor.Data?.Name == vendors.FirstOrDefault(v => v == "Shared"))
        {
            return Total / numOfVendors;
        }

        return Total;
    }
}