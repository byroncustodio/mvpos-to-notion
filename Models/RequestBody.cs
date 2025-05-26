using System;
using Newtonsoft.Json;

namespace mvpos.Models;

public class RequestBody
{
    [JsonProperty("email")]
    private string Email { get; set; }
    
    [JsonProperty("password")]
    private string Password { get; set; }
    
    [JsonProperty("uploadType")]
    private string UploadType { get; set; }
    
    [JsonProperty("notionPageId")]
    private string NotionPageId { get; set; }

    [JsonProperty("fromDate")]
    private DateTime? FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
    
    [JsonProperty("toDate")]
    private DateTime? ToDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddSeconds(-1);
    
    [JsonProperty("limit")]
    private int? Limit { get; set; }

    [JsonProperty("debug")]
    private int Debug { get; set; }
}