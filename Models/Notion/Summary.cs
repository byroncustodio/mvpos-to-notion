using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Summary : Page
{
    public Summary(Page page) : base(page)
    {
        Properties = page.Properties.ToObject<SummaryProperties>();
    }
        
    [JsonProperty("properties")]
    public new SummaryProperties Properties { get; set; }
}