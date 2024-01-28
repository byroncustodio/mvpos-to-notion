using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion
{
    public class AnalysisProperties
    {
        public Date Date { get; set; }

        public LocationProp Location { get; set; }
    }

    public class Date : PageProperty
    {
        [JsonProperty("date")]
        public Properties.Date Data { get; set; }
    }

    public class LocationProp : PageProperty
    {
        [JsonProperty("relation")]
        public List<Relation> Relations { get; set; }
    }
}
