using System.Text.Json;

namespace IDP.WebApp;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CookieStorageService _cookieStorageService;

    public AuthenticationService(HttpClient httpClient, 
        CookieStorageService cookieStorageService)
    {
        _httpClient = httpClient;
        _cookieStorageService = cookieStorageService;
    }

    public async Task<bool> Authenticate(AuthRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("https://localhost:5001/authenticate", request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthResponse>(content);

            if (!string.IsNullOrEmpty(result?.AuthorizationCode))
            {
                _cookieStorageService.SaveAuthorizationCode(result.AuthorizationCode);
                return true;
            }
        }
        return false;
    }

    public async Task ExchangeCodeForToken(string authCode)
    {
        var requestData = new Dictionary<string, string>
        {
            { "GrantType", "authorization_code" },
            { "ClientId", "your-client-id" },
            { "redirect_uri", "https://localhost:5001/auth/callback" },
            { "code", authCode }
        };

        var response = await _httpClient.PostAsync("https://localhost:5001/token", new FormUrlEncodedContent(requestData));
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        if (!string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            _cookieStorageService.SaveToken(tokenResponse.AccessToken);
        }
    }

    public void SaveToken(string token)
    {
        _cookieStorageService.SaveToken(token);
    }

    public void MarkUserAsLoggedOut()
    {
        _cookieStorageService.Logout();
    }
}

public class AuthRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string CodeChallenge { get; set; }
    public string CodeChallengeMethod { get; set; } = "SHA-256";
}

public class AuthResponse
{
    public string AuthorizationCode { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}
