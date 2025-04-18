using IDP.Service.Security;

namespace IDP.Service.Application.TokenService;

public class TokenValidatorService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleRepo _roleRepo;
    private readonly IAppLogger<TokenValidatorService> _logger;
    private readonly ClientService _clientService;
    private readonly AuthorizationRepo _authorizationRepo;

    public TokenValidatorService(UserManager<User> userManager,
        RoleRepo roleRepo,
        ClientService clientService,
        IAppLogger<TokenValidatorService> appLogger,
        AuthorizationRepo authorizationRepo)
    {
        _userManager = userManager;
        _roleRepo = roleRepo;
        _clientService = clientService;
        _logger = appLogger;
        _authorizationRepo = authorizationRepo;
    }

    public async Task<UserTokenInfo> ValidatePkceAndAuthorizeAsync(TokenRequest tokenRequest, string ipAddress)
    {
        _logger.LogInfo("Token request received for ClientId: {ClientId} with Code: {Code}",
            tokenRequest.ClientId, tokenRequest.Code?.SubstringSafe(0, 5));

        var authorizationCode = await _authorizationRepo
            .ValidateAuthorizationCode(tokenRequest.Code, tokenRequest.UserId);

        var calculatedCodeChallenge = PkceHelper.CalculateCodeChallenge(tokenRequest.CodeVerifier);

        if (calculatedCodeChallenge != authorizationCode.CodeChallenge)
        {
            _logger.LogWarning("Invalid code verifier for ClientId: {ClientId}, UserId: {UserId}",
                tokenRequest.ClientId, authorizationCode.UserId);
            throw new UnauthorizedAccessException("Invalid code verifier.");
        }

        var isValidClient = await _clientService.IsValidClient(tokenRequest.ClientId);

        if (!isValidClient)
        {
            _logger.LogWarning("ClientId: {ClientId} is invalid", tokenRequest.ClientId);
            throw new NotFoundException("Client not found.");
        }

        var user = await _userManager.FindByIdAsync(authorizationCode.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found with Id: {UserId}", authorizationCode.UserId);
            throw new NotFoundException("User not found.");
        }

        _logger.LogInfo("User found: {UserName}", user.UserName);

        var userRoles = await _roleRepo.GetUserRoles(user.Id);

        if (!userRoles.IsSafe())
        {
            _logger.LogWarning("No valid roles found for UserId: {UserId}", user.Id);
            throw new NotFoundException("Roles not found.");
        }

        _logger.LogInfo("Roles found for UserId: {UserId} => {Roles}", user.Id, string.Join(", ", userRoles));

        return UserTokenInfo.Create(user.Id,
            user.TenantId,
            user.UserName,
            authorizationCode.ClientId,
            userRoles.Distinct().ToArray());
    }
}
