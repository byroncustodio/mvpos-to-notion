﻿using Newtonsoft.Json;
using NotionSDK.Models;

namespace mvpos.Models.Notion;

public class Summary(Page page) : Page(page)
{
    [JsonProperty("properties")]
    public new SummaryProperties Properties { get; set; } = page.Properties.ToObject<SummaryProperties>();
}