using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Inventory : Page
{
    public Inventory(Page page) : base(page)
    {
        Properties = page.Properties.ToObject<InventoryProperties>();
    }
        
    [JsonProperty("properties")]
    public new InventoryProperties Properties { get; set; }
}