using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion
{
    public class AnalyticProperties
    {
        public Month Month { get; set; }

        public LocationProp Location { get; set; }
    }

    public class Month : PageProperty
    {
        [JsonProperty("title")]
        public List<RichText> Title { get; set; }
    }

    public class LocationProp : PageProperty
    {
        [JsonProperty("relation")]
        public List<Relation> Relations { get; set; }
    }
}
