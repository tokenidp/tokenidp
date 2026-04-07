using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.ApiResources.UseCases;

internal sealed class ApiResourceQueryUseCase
{
    private readonly IApiResourceRepository _apiResourceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ApiResourceQueryUseCase> _logger;

    public ApiResourceQueryUseCase(
        IApiResourceRepository apiResourceRepository,
        ICurrentUserService currentUserService,
        IAppLogger<ApiResourceQueryUseCase> logger)
    {
        _apiResourceRepository = apiResourceRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<List<ApiResourceDetail>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching ApiResources for tenant {TenantId}", _currentUserService.TenantId);

        var items = await _apiResourceRepository.GetApiResourcesAsync(
            _currentUserService.TenantId,
            cancellationToken);

        return ApiResult<List<ApiResourceDetail>>.Success(items);
    }

    public async Task<ApiResult<ApiResourceDetail>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _apiResourceRepository.GetApiResourceDetailAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (item == null)
        {
            return ApiResult<ApiResourceDetail>.Failure(
                ApiError.Failure("api_resource.not_found", $"ApiResource not found for Id {id}"));
        }

        return ApiResult<ApiResourceDetail>.Success(item);
    }
}

