using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Server.Security;

public sealed class SystemTenantRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "system-tenant";
}
