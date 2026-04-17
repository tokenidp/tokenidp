using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Tenants.UseCases;

internal sealed class TenantQueryUseCase
{
    private const int SystemTenantId = 1;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantQueryUseCase> _logger;
    private readonly ISecretProtector _secretProtector;

    public TenantQueryUseCase(
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService,
        IAppLogger<TenantQueryUseCase> logger,
        ISecretProtector secretProtector)
    {
        _tenantRepository = tenantRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _secretProtector = secretProtector;
    }

    public async Task<ApiResult<TenantDetail>> GetTenantById(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tenant {TenantId}", tenantId);

        if (IsCrossTenantAccessDenied(tenantId))
        {
            return ApiResult<TenantDetail>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _tenantRepository.GetTenantDetailAsync(tenantId, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
            return ApiResult<TenantDetail>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        return ApiResult<TenantDetail>.Success(tenant);
    }

    public async Task<ApiResult<RevealTenantProviderSecretResponse>> RevealTenantProviderSecret(
        int tenantId,
        RevealTenantProviderSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        if (IsCrossTenantAccessDenied(tenantId))
        {
            return ApiResult<RevealTenantProviderSecretResponse>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _tenantRepository.GetTenantWithProvidersAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            return ApiResult<RevealTenantProviderSecretResponse>.Failure(
                ApiError.Failure("NotFound", "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        var provider = tenant.TenantExternalProviders
            .FirstOrDefault(p => p.ProviderType == request.ProviderType);

        if (provider?.OidcConfig?.ClientSecret is null || provider.OidcConfig.ClientSecret == string.Empty)
        {
            return ApiResult<RevealTenantProviderSecretResponse>.Failure(
                ApiError.Failure("tenant.provider.secret.notfound",
                    "Secret is not configured for provider {0}.".FormatString(request.ProviderType)));
        }

        var decryptedSecret = _secretProtector.Decrypt(
            provider.OidcConfig.ClientSecret,
            BuildSecretContext(tenant.Id.ToString(), request.ProviderType));

        _logger.LogInfo(
            "Tenant secret revealed. UserId={UserId}, TenantId={TenantId}, Provider={Provider}, TimestampUtc={TimestampUtc}, IP={IpAddress}",
            _currentUserService.UserId,
            tenantId,
            request.ProviderType.ToString(),
            DateTime.UtcNow,
            _currentUserService.IpAddress ?? "unknown");

        return ApiResult<RevealTenantProviderSecretResponse>.Success(
            new RevealTenantProviderSecretResponse
            {
                ProviderType = request.ProviderType,
                ClientSecret = decryptedSecret ?? string.Empty
            });
    }

    public async Task<ApiResult<PaginatedList<TenantSearchResult>>> GetTenants(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tenants list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var tenants = await _tenantRepository.SearchTenantsAsync(
            GetScopedTenantId(),
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} tenants", tenants.TotalCount);

        return ApiResult<PaginatedList<TenantSearchResult>>.Success(tenants);
    }

    private static string BuildSecretContext(string tenantId, ExternalProviderTypes providerType)
    {
        return $"tenant:{tenantId}:provider:{providerType}";
    }

    private int? GetScopedTenantId()
    {
        return HasGlobalTenantAccess()
            ? null
            : _currentUserService.TenantId;
    }

    private bool IsCrossTenantAccessDenied(int tenantId)
    {
        return !HasGlobalTenantAccess()
               && _currentUserService.TenantId > 0
               && tenantId != _currentUserService.TenantId;
    }

    private bool HasGlobalTenantAccess()
    {
        return _currentUserService.TenantId <= 0 || _currentUserService.TenantId == SystemTenantId;
    }
}

