namespace IDP.Web.Model;

public class ApiError
{
    public string CorrelationId { get; set; }
    public int UserId { get; set; }
    public string IPAddress { get; set; }
    public string SourceType { get; set; }
    public Dictionary<string, string> Errors { get; set; }
    public string Help { get; set; }
}