using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Summary : Database
{
    public Summary(Database database) : base(database)
    {
        Properties = database.Properties.ToObject<SummaryProperties>();
    }
        
    [JsonProperty("properties")]
    public new SummaryProperties Properties { get; set; }
}