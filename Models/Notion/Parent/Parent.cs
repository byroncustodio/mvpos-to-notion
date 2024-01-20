using Newtonsoft.Json;

namespace ShopMakersManager.Models.Notion.Parent
{
    public class Base
    {
        [JsonProperty("type")]
        public required string Type { get; set; }
    }
}
