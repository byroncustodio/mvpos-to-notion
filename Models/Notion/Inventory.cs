using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Inventory : Database
{
    public Inventory(Database database) : base(database)
    {
        Properties = database.Properties.ToObject<InventoryProperties>();
    }
        
    [JsonProperty("properties")]
    public new InventoryProperties Properties { get; set; }
}