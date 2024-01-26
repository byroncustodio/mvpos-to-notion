using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class PageProperty
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
