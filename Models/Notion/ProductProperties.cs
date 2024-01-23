using Newtonsoft.Json;
using MakersManager.Models.Notion.Block;
using System.Collections.Generic;

namespace MakersManager.Models.Notion
{
    public class ProductProperties
    {
        public Sku SKU { get; set; }

        public Vendor Vendor { get; set; }
    }

    public class Sku
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("rich_text")]
        public List<RichText> RichText { get; set; }
    }

    public class Vendor
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("select")]
        public Select Select { get; set; }
    }
}
