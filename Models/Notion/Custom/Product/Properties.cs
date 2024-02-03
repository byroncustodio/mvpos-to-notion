using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion.Custom.Product
{
    public class Properties
    {
        public Name Name { get; set; }

        public Sku SKU { get; set; }

        public Vendor Vendor { get; set; }

        public Alias Alias { get; set; }
    }

    public class Name : PageProperty
    {
        [JsonProperty("title")]
        public List<RichText> Title { get; set; }
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

    public class Alias : PageProperty
    {
        [JsonProperty("rich_text")]
        public List<RichText> RichText { get; set; }
    }
}
