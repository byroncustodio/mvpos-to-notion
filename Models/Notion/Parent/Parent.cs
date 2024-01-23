using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Parent
{
    public class Base
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
