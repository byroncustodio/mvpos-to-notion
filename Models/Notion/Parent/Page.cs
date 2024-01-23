using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Parent
{
    public class Page : Base
    {
        [JsonProperty("page_id")]
        public string PageId { get; set; }
    }
}
