using Newtonsoft.Json;

namespace Identity.Application.PowerBI;

public class PowerBIResponse
{
    public string Expiration { get; set; }
    public string Token { get; set; }
    public string TokenId { get; set; }
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
    public IEnumerable<BIReport> Reports { get; set; }

    public PowerBIResponse()
    {

    }

    public PowerBIResponse(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public void SetResponse(IEnumerable<BIReport> reports)
    {
        Reports = reports;
        IsSuccess = true;
    }
}

public class BIReport
{
    public string ReportId { get; set; }
    public string EmbedUrl { get; set; }
    public string DatasetId { get; set; }

    public BIReport(string id, string dsId, string url)
    {
        ReportId = id;
        EmbedUrl = url;
        DatasetId = dsId;
    }
}

public class BIReportResponse
{
    [JsonProperty("id")]
    public string ReportId { get; set; }
    [JsonProperty("datasetId")]
    public string DatasetId { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("embedUrl")]
    public string EmbedUrl { get; set; }
    [JsonProperty("webUrl")]
    public string WebUrl { get; set; }
}
