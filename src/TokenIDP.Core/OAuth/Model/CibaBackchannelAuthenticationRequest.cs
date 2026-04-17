using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TokenIDP.Core.OAuth.Model;

public sealed class CibaBackchannelAuthenticationRequest
{
    [Required]
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;

    [JsonPropertyName("login_hint")]
    public string? LoginHint { get; init; }

    [JsonPropertyName("login_hint_token")]
    public string? LoginHintToken { get; init; }

    [JsonPropertyName("id_token_hint")]
    public string? IdTokenHint { get; init; }

    [JsonPropertyName("binding_message")]
    public string? BindingMessage { get; init; }

    [JsonPropertyName("user_code")]
    public string? UserCode { get; init; }

    [JsonPropertyName("requested_expiry")]
    public int? RequestedExpiry { get; init; }

    [JsonPropertyName("acr_values")]
    public string? AcrValues { get; init; }

    [JsonPropertyName("client_notification_token")]
    public string? ClientNotificationToken { get; init; }

    public string ClientId { get; private set; } = string.Empty;
    public string? ClientSecret { get; private set; }
    public string? ClientAuthenticationMethod { get; private set; }
    public int TenantId { get; private set; }

    public void SetClientAuthentication(string clientId, string? clientSecret, string authenticationMethod)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        ClientAuthenticationMethod = authenticationMethod;
    }

    public void SetTenantId(int tenantId)
    {
        TenantId = tenantId;
    }
}
