namespace Identity.Application.Identity.Authentication;

public class AuthResponse
{
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; }
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public bool IsParentTenant { get; private set; }
    public string UserName { get; private set; }
    public string LandingPage { get; private set; }
    public string Theme { get; private set; }
    public string ReportId { get; private set; }
    public string Token { get; private set; }
    public string RefreshToken { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public IEnumerable<ClaimDto> Claims { get; private set; }

    public AuthResponse(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public void SetResponse(string token,
        int userId,
        int tenantId,
        string name,
        string page,
        string theme,
        string reportId,
        bool isParent,
        IEnumerable<ClaimDto> claims)
    {
        Token = token;
        UserId = userId;
        TenantId = tenantId;
        UserName = name;
        Claims = claims;
        IsParentTenant = isParent;
        ReportId = reportId;
        LandingPage = page;
        Theme = theme;
        IsAuthenticated = !string.IsNullOrEmpty(Token);
        RefreshToken = string.Empty;
    }
}
