namespace Identity.Application.PowerBI;

public interface IPowerBIService
{
    Task<string> GetADAccessToken();

    Task<IEnumerable<BIReport>> GetReports(string token, string[] reportIds);

    Task<PowerBIResponse> GetEmbedToken(string token, BIEmbedTokenRequest request);
}
