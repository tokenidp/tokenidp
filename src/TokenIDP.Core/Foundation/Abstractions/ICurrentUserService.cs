namespace TokenIDP.Core.Foundation.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    Guid CorrelationId { get; }
    string UserName { get; }
    string BaseUrl { get; }
    string Scopes { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string[] GetRoles();
}

