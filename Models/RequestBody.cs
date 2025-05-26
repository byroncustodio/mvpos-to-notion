using System;
using System.Collections.Generic;

namespace mvpos.Models;

public class RequestBody
{
    private string Email { get; set; }
    private string Password { get; set; }
    private string UploadType { get; set; }
    private string NotionPageId { get; set; }

    private DateTime FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
    private DateTime ToDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddSeconds(-1);
    private int Limit { get; set; }
    private List<string> Vendors { get; set; } = [];
    private bool Debug { get; set; }
}