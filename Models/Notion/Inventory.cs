using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Inventory(Page page) : Page(page)
{
    [JsonProperty("properties")]
    public new InventoryProperties Properties { get; set; } = page.Properties.ToObject<InventoryProperties>();
}