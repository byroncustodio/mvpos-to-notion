using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Location : Database
{
    public Location(Database database) : base(database)
    {
        Properties = database.Properties.ToObject<LocationProperties>();
    }
    
    [JsonProperty("properties")]
    public new LocationProperties Properties { get; set; }
}