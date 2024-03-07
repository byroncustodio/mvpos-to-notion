using System;
using System.Linq;
using MakersManager.Models.Notion;
using MvposSDK.Models;

namespace MakersManager.Models.Mvpos;

public class CustomSaleItem : SaleItem
{
    public CustomSaleItem(SaleItem saleItem) : base(saleItem) { }

    public decimal Profit
    {
        get
        {
            var numOfVendors = Enum
                .GetNames(typeof(Enums.Vendor))
                .Count(vendor => vendor != Enum.GetName(typeof(Enums.Vendor), Enums.Vendor.Shared));

            if (Product == null)
            {
                if (Price == 25m) // Assume this is a mystery box sale
                {
                    return Total / numOfVendors;
                }

                // Cant determine which vendor, sale is split evenly
                return Total / numOfVendors;
            }

            if (Product.Properties.Vendor.Data.Name == Enum.GetName(typeof(Enums.Vendor), Enums.Vendor.Shared))
            {
                return Total / numOfVendors;
            }

            return Total;
        }
    }
    
    public bool NeedsReview { get; set; }
    
    public Product Product { get; set; }
    
    public Location Location { get; set; }
    
    public Summary Summary { get; set; }
}