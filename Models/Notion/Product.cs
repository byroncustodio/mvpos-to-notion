using Newtonsoft.Json;
using NotionSDK.Models;

namespace MakersManager.Models.Notion;

public class Product : Page
{
    public Product(Page page) : base(page)
    {
        Properties = page.Properties.ToObject<ProductProperties>();
    }
        
    [JsonProperty("properties")]
    public new ProductProperties Properties { get; set; }
}