using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Custom.Summary
{
    public class Summary : Page
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }
}
