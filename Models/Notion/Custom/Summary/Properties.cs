using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion.Custom.Summary
{
    public class Properties
    {
        public Date Date { get; set; }

        public LocationProp Location { get; set; }

        public Status Status { get; set; }
    }

    public class Date : PageProperty
    {
        [JsonProperty("date")]
        public Notion.Properties.Date Data { get; set; }
    }

    public class LocationProp : PageProperty
    {
        [JsonProperty("relation")]
        public List<Relation> Relations { get; set; }
    }

    public class Status : PageProperty
    {
        [JsonProperty("status")]
        public Notion.Properties.Status Data { get; set; }
    }
}
