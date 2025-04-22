using Newtonsoft.Json;
using NotionSDK.Models;

namespace mvpos.Models.Notion;

public class Product(Page page) : Page(page)
{
    [JsonProperty("properties")]
    public new ProductProperties Properties { get; set; } = page.Properties.ToObject<ProductProperties>();
}