namespace IDP.Web.Model;

public class AuthCodeResponse
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
    public string CorrelationId { get; set; }
    public string AuthorizationCode { get; set; }
    public int? UserId { get; set; }
    public bool? TwoFactorEnabled { get; set; }
}
