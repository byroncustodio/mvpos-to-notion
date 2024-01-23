using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Block
{
    public class Select
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
