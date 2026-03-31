namespace Admin.Core.ApiResources.UseCases;

internal sealed class ApiResourceQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ApiResourceQueryUseCase> _logger;

    public ApiResourceQueryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<ApiResourceQueryUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<List<ApiResourceDetail>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching ApiResources for tenant {TenantId}", _currentUserService.TenantId);

        var items = await _dbContext.ApiResources
            .AsNoTracking()
            .Where(x => x.TenantId == _currentUserService.TenantId)
            .OrderBy(x => x.DisplayName)
            .Select(ApiResourceDetail.Projection)
            .ToListAsync(cancellationToken);

        return ApiResult<List<ApiResourceDetail>>.Success(items);
    }

    public async Task<ApiResult<ApiResourceDetail>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.ApiResources
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == _currentUserService.TenantId)
            .Select(ApiResourceDetail.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
        {
            return ApiResult<ApiResourceDetail>.Failure(
                ApiError.Failure("api_resource.not_found", $"ApiResource not found for Id {id}"));
        }

        return ApiResult<ApiResourceDetail>.Success(item);
    }
}
