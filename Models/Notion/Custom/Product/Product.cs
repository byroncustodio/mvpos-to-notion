using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Custom.Product
{
    public class Product : Page
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }
}
