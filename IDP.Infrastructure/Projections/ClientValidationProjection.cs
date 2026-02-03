using IDP.Core.Model;
using System.Linq.Expressions;

namespace IDP.Infrastructure.Projections;

internal static class ClientValidationProjection
{
    public static Expression<Func<Client, ClientValidationResult>> Projection =>
        client => new ClientValidationResult(
            client.RedirectUri,
            client.ClientScopes.Select(s => s.Scope),
            client.ClientGrantTypes.Select(g => g.AllowedGrantType)
        );
}
