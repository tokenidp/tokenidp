namespace TokenIDP.Core.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    string TenantKey { get; }
    int AuthTenantId { get; }
    string AuthTenantKey { get; }
    string ClientId { get; }
    Guid CorrelationId { get; }
    string UserName { get; }
    string BaseUrl { get; }
    string Scopes { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string[] GetRoles();
}

