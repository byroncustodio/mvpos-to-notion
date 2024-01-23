using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class Product : Page
    {
        [JsonProperty("properties")]
        public ProductProperties Properties { get; set; }
    }
}
