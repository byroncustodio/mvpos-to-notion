using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class Page
    {
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
