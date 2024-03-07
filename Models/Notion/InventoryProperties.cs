using NotionSDK.Models.Property;

namespace MakersManager.Models.Notion;

public class InventoryProperties
{
    public Relation Product { get; set; }
    
    public Relation Location { get; set; }
}