using System.Linq.Expressions;

namespace IDP.Infrastructure.Projections;

internal static class UserProjection
{
    public static Expression<Func<User, UserShortInfo>> Projection =>
        user => new UserShortInfo(
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            user.UserName ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber ?? string.Empty,
            user.PhoneNumberConfirmed,
            user.CreatedOn,
            user.UpdatedOn
            );
}
