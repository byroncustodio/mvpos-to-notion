using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Parent
{
    public class Block : Base
    {
        [JsonProperty("block_id")]
        public string BlockId { get; set; }
    }
}
