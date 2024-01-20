using Newtonsoft.Json;

namespace ShopMakersManager.Models.Notion.Parent
{
    public class Page : Base
    {
        [JsonProperty("page_id")]
        public required string PageId { get; set; }
    }
}
