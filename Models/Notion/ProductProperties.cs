using Newtonsoft.Json;
using MakersManager.Models.Notion.Block;
using System.Collections.Generic;
using MakersManager.Models.Notion.Properties;

namespace MakersManager.Models.Notion
{
    public class ProductProperties
    {
        public Sku SKU { get; set; }

        public Vendor Vendor { get; set; }
    }

    public class Sku : PageProperty
    {
        [JsonProperty("rich_text")]
        public List<RichText> RichText { get; set; }
    }

    public class Vendor : PageProperty
    {
        [JsonProperty("select")]
        public Select Select { get; set; }
    }
}
