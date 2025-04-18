using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text.Json;

namespace ClientWebApp.Authentication;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ISessionStorageService _sessionStorage;

    public JwtAuthenticationStateProvider(ISessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _sessionStorage.GetItemAsync<string>("authToken");

        var identity = string.IsNullOrEmpty(token)
            ? new ClaimsIdentity()
            : new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

        var user = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(user);
        return state;
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _sessionStorage.SetItemAsync("authToken", token);

        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        var user = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _sessionStorage.RemoveItemAsync("authToken");

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task StoreCodeVerifierAsync(string codeVerifier)
    {
        await _sessionStorage.SetItemAsync("codeVerifier", codeVerifier);
    }

    public async Task<string> GetCodeVerifierAsync()
    {
        return await _sessionStorage.GetItemAsync<string>("codeVerifier");
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = WebEncoders.Base64UrlDecode(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        return keyValuePairs?.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
    }
}

