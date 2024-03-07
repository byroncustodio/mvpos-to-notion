using NotionSDK.Models.Property;

namespace MakersManager.Models.Notion;

public class SummaryProperties
{
    public Date Date { get; set; }

    public Relation Location { get; set; }

    public Status Status { get; set; }
}