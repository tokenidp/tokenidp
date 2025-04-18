namespace IDP.Service.Application;

public class RoleService
{
    private readonly RoleRepo _roleRepo;
    private readonly IAppLogger<RoleService> _logger;

    public RoleService(RoleRepo roleRepo,
        IAppLogger<RoleService> logger)
    {
        _roleRepo = roleRepo;
        _logger = logger;
    }

    public async Task<bool> HasPermission(int userId, string claim)
    {
        _logger.LogDebug("Checking authorization for user {UserId} and claim {Claim}", userId, claim);

        var hasPermission = await _roleRepo.HasUserPermission(userId, claim);

        return hasPermission;
    }

    public async Task<bool> HasRole(int userId, string role)
    {
        _logger.LogDebug("Checking role membership for user {UserId} and role {Role}", userId, role);

        var hasRole = await _roleRepo.IsUserInRole(userId, role);

        return hasRole;
    }
}