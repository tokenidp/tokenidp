namespace IDP.Core.OAuth.Model;

public class MfaRequest
{
    public string CorrelationId { get; set; }
    public int UserId { get; set; }
    public string Code { get; set; }
}
