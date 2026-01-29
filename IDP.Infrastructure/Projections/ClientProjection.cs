using System.Linq.Expressions;

namespace IDP.Infrastructure.Projections;

internal static class ClientProjection
{
    public static Expression<Func<Client, ClientValidationSnapshot>> ValidationSnapshot =>
        client => new ClientValidationSnapshot(
            client.ClientId,
            client.ClientName,
            client.TenantId,
            client.IsActive,
            client.RedirectUri,
            client.LogoutRedirectUri,
            client.ClientType,
            client.TokenType,
            client.ClientGrantTypes.Select(g => g.AllowedGrantType),
            client.ClientScopes.Select(s => s.Scope),
            client.ClientAudiences
                .Where(a => a.IsActive != false)
                .Select(a => a.Name),
            client.ClientSecrets
                .Where(s => s.ExpiresAt > DateTime.UtcNow && s.IsRevoked != true)
                .Select(s => s.SecretHash),
            client.AccessTokenLifetime,
            client.AuthorizationCodeLifetime,
            client.RefreshTokenExpiration,
            client.ClientSecretExpiry
        );
}

