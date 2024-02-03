using MakersManager.Models.Notion.Custom.Summary;
using MakersManager.Models.Notion.Custom.Location;
using MakersManager.Models.Notion.Custom.Product;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MakersManager.Models.MVPOS
{
    public class SaleItems
    {
        [JsonProperty("sale_items")]
        public List<SaleItem> Items { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }
    }

    public class SaleItem
    {
        [JsonProperty("sale_id")]
        public int SaleId { get; set; }

        [JsonProperty("sale_date")]
        public DateTime SaleDate { get; set; }

        [JsonProperty("payment_method")]
        public Payment Payment { get; set; }
        public string PaymentName
        {
            get
            {
                return Payment != null ? Payment.Name : string.Empty;
            }
        }

        [JsonProperty("client_location_id")]
        public int LocationId { get; set; }
        public string LocationName
        {
            get
            {
                return ((MakersManager.MVPOS.StoreLocation)LocationId).ToString();
            }
        }
        public Location Location { get; set; }

        [JsonProperty("item_number")]
        public string Sku { get; set; }

        [JsonIgnore]
        public Product Product { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("subtotal")]
        public decimal SubTotal { get; set; }

        [JsonProperty("discount_percentage")]
        public decimal Discount { get; set; }

        [JsonProperty("tax_amount")]
        public decimal Tax { get; set; }

        [JsonProperty("final_cost")]
        public decimal Total { get; set; }

        [JsonIgnore]
        public decimal Profit
        {
            get
            {
                var numOfVendors = Enum.GetNames(typeof(MakersManager.MVPOS.Vendor))
                    .ToList()
                    .Where(vendor => vendor != Enum.GetName(typeof(MakersManager.MVPOS.Vendor), MakersManager.MVPOS.Vendor.Shared))
                    .Count();

                if (Product == null)
                {
                    if (Price == 25m) // Assume this is a mystery box sale
                    {
                        return Total / numOfVendors;
                    }
                    else // Cant determine which vendor, sale is split evenly
                    {
                        return Total / numOfVendors;
                    }
                }
                else if (Product.Properties.Vendor.Select.Name == Enum.GetName(typeof(MakersManager.MVPOS.Vendor), MakersManager.MVPOS.Vendor.Shared))
                {
                    return Total / numOfVendors;
                }
                else
                {
                    return Total;
                }
            }
        }

        [JsonIgnore]
        public Summary Summary { get; set; }

        [JsonIgnore]
        public bool NeedsReview { get; set; }
    }

    public class Payment
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }
    }
}
