using Newtonsoft.Json;

namespace MakersManager.Models.Notion.Properties
{
    public class Relation
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
