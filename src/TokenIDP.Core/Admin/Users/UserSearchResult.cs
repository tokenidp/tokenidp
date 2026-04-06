namespace TokenIDP.Core.Admin.Users;

internal class UserSearchResult
{
    internal static Expression<Func<UserSearch, UserSearchResult>> Projection =>
        user => new UserSearchResult()
        {
            Id = user.Id,
            TenantId = user.TenantId,
            FullName = user.FullName,
            UserName = user.UserName,
            Status = user.Status,
            FullAddress = user.FullAddress,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Roles = user.Roles,
            UpdatedBy = user.UpdatedBy
        };

    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string FullName { get; private set; } = default!;
    public string UserName { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string FullAddress { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Roles { get; private set; } = default!;
    public string UpdatedBy { get; private set; } = default!;
}

