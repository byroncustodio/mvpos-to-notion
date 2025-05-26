using mvpos.Models.Notion;
using MvposSDK.Models;
using Location = mvpos.Models.Notion.Location;

namespace mvpos.Models.Mvpos;

public class CustomSaleItem(SaleItem saleItem) : SaleItem(saleItem)
{
    public bool NeedsReview { get; set; }

    public Product Product { get; set; }

    public Location Location { get; set; }

    public Summary Summary { get; set; }

    public Inventory Inventory { get; set; }

    public decimal GetProfit()
    {
        return Total;
    }
}