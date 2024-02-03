using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Custom.Location
{
    public class Location : Page
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }
}
