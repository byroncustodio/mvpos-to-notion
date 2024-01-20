using Newtonsoft.Json;

namespace ShopMakersManager.Models.Notion.Parent
{
    public class Block : Base
    {
        [JsonProperty("block_id")]
        public required string BlockId { get; set; }
    }
}
