using TokenIDP.Domain.AggregateRoots.Tokens;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Tokens.UseCases;

internal sealed class TokenLookupsUseCase
{
    private readonly ITokenRepository _tokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TokenLookupsUseCase> _logger;

    public TokenLookupsUseCase(
        ITokenRepository tokenRepository,
        ICurrentUserService currentUserService,
        IAppLogger<TokenLookupsUseCase> logger)
    {
        _tokenRepository = tokenRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<TokenLookups>> GetLookups(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching token lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _tokenRepository.GetTokenLookupsAsync(
            _currentUserService.TenantId,
            cancellationToken);

        return ApiResult<TokenLookups>.Success(lookups);
    }
}
