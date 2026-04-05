
internal sealed class SystemCurrentUserService : ICurrentUserService
{
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string UserName => "system";
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public string BaseUrl => throw new NotImplementedException();
    public string Scopes => throw new NotImplementedException();
    public string? IpAddress => throw new NotImplementedException();
    public string? UserAgent => throw new NotImplementedException();
    public string[] GetRoles()
    {
        throw new NotImplementedException();
    }

    public SystemCurrentUserService()
    {
        UserId = 0;     // means: system / bootstrap
        TenantId = 0;   // means: not yet known
    }

    public void SetTenant(int tenantId) => TenantId = tenantId;
    public void SetUser(int userId) => UserId = userId;
}