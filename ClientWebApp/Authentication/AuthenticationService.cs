using ClientWebApp.Model;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientWebApp.Authentication;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _jwtAuthenticationStateProvider;

    public AuthenticationService(IHttpClientFactory httpClientFactory,
        JwtAuthenticationStateProvider jwtAuthenticationStateProvider)
    {
        _httpClient = httpClientFactory.CreateClient("IDPClient");
        _jwtAuthenticationStateProvider = jwtAuthenticationStateProvider;
    }

    public async Task<string?> ExchangeCodeForToken(string authCode)
    {
        var codeVerifier = await _jwtAuthenticationStateProvider.GetCodeVerifierAsync();
        TokenRequest request = new()
        {
            GrantType = "authorization_code",
            ClientId = "123456789",
            RedirectUri = "https://localhost:7202/Counter",
            Code = authCode,
            CodeVerifier = codeVerifier
        };

        var response = await _httpClient.PostAsJsonAsync("token", request);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<Result<TokenResponse>>(content);

        if (!string.IsNullOrEmpty(tokenResponse?.Value.AccessToken))
        {
            await _jwtAuthenticationStateProvider
                .MarkUserAsAuthenticated(tokenResponse.Value.AccessToken);

            var userInforequest = new HttpRequestMessage(HttpMethod.Get, $"authenticate/userinfo/{tokenResponse.Value.UserId}");
            userInforequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Value.AccessToken);

            var userInfoResponse = await _httpClient.SendAsync(userInforequest);

            if (userInfoResponse.IsSuccessStatusCode)
            {
                var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<Result<UserInfo>>();

                return userInfo?.Value.UserName;
            }
            else
            {
                // Handle errors (e.g., unauthorized, not found, etc.)
                Console.WriteLine($"Error: {userInfoResponse.StatusCode}");
            }
        }

        return default;
    }
}