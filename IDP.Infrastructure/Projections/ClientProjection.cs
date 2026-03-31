using IDP.Core.Model;
using System.Linq.Expressions;

namespace IDP.Infrastructure.Projections;

internal static class ClientShortInfoProjection
{
    public static Expression<Func<Client, ClientShortInfo>> Projection =>
        client => new ClientShortInfo
        (
            client.Id,
            client.TenantId,
            client.ClientAuthPolicy.AllowForgotPassword,
            client.ClientName,
            client.RedirectUri,
            client.RequiredPkce,
            client.ClientScopes.Select(s => s.Scope),
            client.ClientGrantTypes.Select(g => g.AllowedGrantType)
        );
}