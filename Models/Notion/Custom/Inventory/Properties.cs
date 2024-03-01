using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion.Custom.Inventory
{
    public class Properties
    {
        public ProductProp Product { get; set; }
        public LocationProp Location { get; set; }
    }

    public class ProductProp : PageProperty
    {
        [JsonProperty("relation")]
        public List<Relation> Relations { get; set; }
    }

    public class LocationProp : PageProperty
    {
        [JsonProperty("relation")]
        public List<Relation> Relations { get; set; }
    }
}
