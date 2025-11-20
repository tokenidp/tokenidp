using Admin.Core;
using Identity.Application.PowerBI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Identity;

public class PowerBIService : IPowerBIService
{
    private readonly PowerBISetting _setttings;
    private readonly IRestClient _restClient;
    private readonly IConfiguration _configuration;
    private readonly ICache _cache;

    public PowerBIService(IOptions<PowerBISetting> setttings,
        IRestClient restClient,
        IConfiguration configuration,
        ICache cache)
    {
        _setttings = setttings.Value;
        _restClient = restClient;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<string> GetADAccessToken()
    {
        var accessToken = _cache.GetValue<ADAccessToken>(CacheKeys.AD_ACCESS_TOKEN);

        if (accessToken != null
            && accessToken.ExpiryDate.Value.AddMinutes(-5) <= DateTime.Now)
        {
            var credentials = _configuration
                .GetSection("ADCredentials").GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);

            accessToken = await _restClient
                .Authenticate<ADAccessToken>(_setttings.MicrosoftAuthUrl, credentials);

            if (accessToken != null)
            {
                accessToken.SetExpire(accessToken.ExpiresIn);

                _cache.Add(CacheKeys.AD_ACCESS_TOKEN, accessToken);
            }
        }

        return accessToken?.AccessToken;
    }

    public async Task<PowerBIResponse> GetEmbedToken(string token, BIEmbedTokenRequest request)
    {
        var response = await _restClient.Post<PowerBIResponse, BIEmbedTokenRequest>
            (_setttings.EmbedTokenUrl, token, request);

        return response;
    }

    public async Task<IEnumerable<BIReport>> GetReports(string token, string[] reportIds)
    {
        List<BIReport> reports = new();

        foreach (var reportId in reportIds)
        {
            var response = await _restClient.Get<BIReportResponse>
                (_setttings.ReportUrl.FormatString(reportId), token);

            if (response != null)
            {
                reports.Add(new(response.ReportId, response.DatasetId, response.EmbedUrl));
            }
        }

        return reports;
    }
}
