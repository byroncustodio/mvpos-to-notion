using NotionSDK.Models.Property;

namespace mvpos.Models.Notion;

public class ProductProperties
{
    public Title Name { get; set; }

    public RichText Sku { get; set; }

    public Select Vendor { get; set; }

    public RichText Alias { get; set; }
}