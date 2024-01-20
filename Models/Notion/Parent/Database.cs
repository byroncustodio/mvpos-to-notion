using Newtonsoft.Json;

namespace ShopMakersManager.Models.Notion.Parent
{
    public class Database : Base
    {
        [JsonProperty("database_id")]
        public required string DatabaseId { get; set; }
    }
}
