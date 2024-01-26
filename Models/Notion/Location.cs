using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class Location : Page
    {
        [JsonProperty("properties")]
        public LocationProperties Properties { get; set; }
    }
}
