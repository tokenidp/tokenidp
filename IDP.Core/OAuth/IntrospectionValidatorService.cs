using IDP.Core.Model;

namespace IDP.Core.OAuth;

internal sealed class IntrospectionValidatorService
{
    private readonly IAppLogger<RevokeTokenService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public IntrospectionValidatorService(ApplicationDbContext dbContext,
        IAppLogger<RevokeTokenService> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IntrospectionResponse> ValidateReferenceToken(string referenceToken)
    {
        _logger.LogDebug("Validating reference token: {TokenId}", referenceToken);

        var accessToken = await _dbContext.UserAccessToken
            .FirstOrDefaultAsync(s => s.ReferenceToken == referenceToken && s.IsRevoked != true);

        if (accessToken == null)
        {
            _logger.LogWarning("Reference token not found or revoked: {TokenId}",
                $"{referenceToken.SubstringSafe(0, 5)}...");

            return IntrospectionResponse.Create();
        }

        _logger.LogDebug("Valid token found for user {UserId}", accessToken.UserId);

        return IntrospectionResponse.Create(
            accessToken.UserId,
            accessToken.TenantId,
            accessToken.Scopes,
            accessToken.Roles.Split(","));
    }
}
