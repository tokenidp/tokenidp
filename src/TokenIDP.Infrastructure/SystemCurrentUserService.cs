﻿
using TokenIDP.Core.Abstractions;

internal sealed class SystemCurrentUserService : ICurrentUserService
{
    private readonly string _baseUrl;

    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string TenantKey { get; set; } = string.Empty;
    public int AuthTenantId => TenantId;
    public string AuthTenantKey => TenantKey;
    public string ClientId => string.Empty;
    public string UserName => "system";
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public string BaseUrl => _baseUrl;
    public string Scopes => string.Empty;
    public string? IpAddress => null;
    public string? UserAgent => null;
    public string[] GetRoles()
    {
        return Array.Empty<string>();
    }

    public SystemCurrentUserService(string baseUrl = "")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        UserId = 0;     // means: system / bootstrap
        TenantId = 0;   // means: not yet known
    }

    public void SetTenant(int tenantId) => TenantId = tenantId;
    public void SetUser(int userId) => UserId = userId;
}
