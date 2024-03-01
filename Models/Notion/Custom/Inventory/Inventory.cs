using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Custom.Inventory
{
    public class Inventory : Page
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }
}
