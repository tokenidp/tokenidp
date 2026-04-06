namespace TokenIDP.Core.OAuth.Model;

public class MfaRequest
{
    public string CorrelationId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}

