using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Product : Database
{
    public Product(Database database) : base(database)
    {
        Properties = database.Properties.ToObject<ProductProperties>();
    }
        
    [JsonProperty("properties")]
    public new ProductProperties Properties { get; set; }
}