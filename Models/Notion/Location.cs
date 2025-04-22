using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Location(Page page) : Page(page)
{
    [JsonProperty("properties")]
    public new LocationProperties Properties { get; set; } = page.Properties.ToObject<LocationProperties>();
}