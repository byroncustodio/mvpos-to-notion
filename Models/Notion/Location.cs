using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Location : Page
{
    public Location(Page page) : base(page)
    {
        Properties = page.Properties.ToObject<LocationProperties>();
    }
    
    [JsonProperty("properties")]
    public new LocationProperties Properties { get; set; }
}