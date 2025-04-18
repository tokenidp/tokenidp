using IDP.Service.Model;
using IDP.Web.Model;
using System.Net;

namespace IDP.Web;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;

    public AuthenticationService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
    }

    public async Task<Result<AuthCodeResponse>> Authenticate(AuthRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("authenticate", request);

        if (response.StatusCode == HttpStatusCode.BadRequest
            || response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<Result<ApiError>>();
            throw new Microsoft.AspNetCore.Http.BadHttpRequestException(errorResponse.ErrorMessage, (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<Result<AuthCodeResponse>>();
        return result;
    }

    public async Task<ClientDto> GetClient(string clientId)
    {
        var response = await _httpClient.GetFromJsonAsync<Result<ClientDto>>($"client/{clientId}");

        return response?.Value;
    }
}