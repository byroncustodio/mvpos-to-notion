using Newtonsoft.Json;
using NotionSDK.Models.Property;

namespace MakersManager.Models.Notion;

public class InventoryProperties
{
    public Relation Product { get; set; }
    
    public Relation Location { get; set; }
    
    [JsonProperty("Last Restock")]
    
    public Date LastRestock { get; set; }
    
    [JsonProperty("New Stock")]
    
    public Number NewStock { get; set; }
}