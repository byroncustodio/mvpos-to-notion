using Newtonsoft.Json;

namespace MakersManager.Models.Notion
{
    public class Analysis : Page
    {
        [JsonProperty("properties")]
        public AnalysisProperties Properties { get; set; }
    }
}
