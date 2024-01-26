using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class Analytic : Page
    {
        [JsonProperty("properties")]
        public AnalyticProperties Properties { get; set; }
    }
}
