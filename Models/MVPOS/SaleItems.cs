using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ShopMakersManager.Models.MVPOS
{
    public class SaleItems
    {
        [JsonProperty("sale_items")]
        public List<SaleItem> Items { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }

        public SaleItems() { }
    }

    public class SaleItem
    {
        [JsonProperty("sale_id")]
        public int SaleId { get; set; }

        [JsonProperty("sale_date")]
        public DateTime SaleDate { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName
        {
            get
            {
                return PaymentMethod != null ? PaymentMethod.Name : string.Empty;
            }
        }

        [JsonProperty("client_location_id")]
        public int LocationId { get; set; }
        public string LocationName
        {
            get
            {
                return LocationId switch
                {
                    212 => "Gastown",
                    213 => "Kitsilano",
                    214 => "North Vancouver",
                    215 => "Victoria",
                    216 => "Metrotown",
                    217 => "Guildford",
                    252 => "Tsawwassen",
                    253 => "Richmond",
                    261 => "Park Royal",
                    262 => "Southgate",
                    _ => string.Empty,
                };
            }
        }

        [JsonProperty("item_number")]
        public string Sku { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
        public string PriceFormatted
        {
            get
            {
                return Price.ToString("C");
            }
        }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("subtotal")]
        public decimal SubTotal { get; set; }
        public string SubTotalFormatted
        {
            get
            {
                return SubTotal.ToString("C");
            }
        }

        [JsonProperty("discount_percentage")]
        public decimal DiscountPercent { get; set; }
        public string DiscountPercentFormatted
        {
            get
            {
                return (decimal.ToDouble(DiscountPercent) / 100d).ToString("P");
            }
        }

        [JsonProperty("tax_amount")]
        public decimal TaxAmount { get; set; }
        public string TaxAmountFormatted
        {
            get
            {
                return TaxAmount.ToString("C");
            }
        }

        [JsonProperty("final_cost")]
        public decimal FinalCost { get; set; }
        public string FinalCostFormatted
        {
            get
            {
                return FinalCost.ToString("C");
            }
        }

        public SaleItem() { }
    }

    public class PaymentMethod
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }

        public PaymentMethod() { }
    }
}
