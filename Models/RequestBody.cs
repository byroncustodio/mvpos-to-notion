using System;
using Newtonsoft.Json;

namespace mvpos.Models;

public class RequestBody
{
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("password")]
    public string Password { get; set; }

    [JsonProperty("uploadType")]
    public string UploadType { get; set; }

    [JsonProperty("notionPageId")]
    public string NotionPageId { get; set; }
    
    [JsonProperty("range")]
    public Range Range { get; set; } = new();

    [JsonProperty("limit")]
    public int Limit { get; set; }
    
    [JsonProperty("locations")]
    public string Locations { get; set; }

    [JsonProperty("debug")]
    public int Debug { get; set; }
}

public class Range
{
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("from")]
    public DateTime From { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);

    [JsonProperty("to")]
    public DateTime To { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddSeconds(-1);
}