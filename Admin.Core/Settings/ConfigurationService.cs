using IDP.Domain.AggregateRoots;

namespace Admin.Core.Configurations;

internal class ConfigurationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ConfigurationService> _logger;

    public ConfigurationService(IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<ConfigurationService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> CreateConfiguration(CreateUpdateConfiguration request)
    {
        _logger.LogDebug("Creating configuration {ConfigKey} for tenant {TenantId}",
            request.ConfigKey, _currentUserService.TenantId);

        Configuration configuration = new(_currentUserService.TenantId,
            request.ConfigKey,
            request.ConfigValue,
            request.IsDisplay,
            request.IsEditable);

        _dbContext.Configurations.Add(configuration);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Configuration created with Id {ConfigId}", configuration.Id);

        return Result.Success(result);
    }

    public async Task<Result> UpdateConfiguration(int id, CreateUpdateConfiguration request)
    {
        _logger.LogDebug("Updating configuration {ConfigId}", id);

        var configuration = await _dbContext.Configurations.FirstOrDefaultAsync(c => c.Id == id);

        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found for update: {ConfigId}", id);
            return Result.Failure("NotFound", "Configuration not found for the Id {0}".FormatString(id));
        }

        configuration.UpdateConfiguration(
            request.ConfigValue,
            request.IsDisplay,
            request.IsEditable);

        _dbContext.Configurations.Update(configuration);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Configuration updated {ConfigId}", id);

        return Result.Success(result);
    }

    public async Task<ConfigurationDto> GerConfigurationById(int configId)
    {
        _logger.LogDebug("Fetching configuration {ConfigId}", configId);

        var configuration = await _dbContext.Configurations
            .Where(u => u.Id == configId)
            .Select(ConfigurationDto.Projection)
            .FirstOrDefaultAsync();

        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found: {ConfigId}", configId);
        }

        return configuration;
    }

    public async Task<PaginatedList<ConfigurationSearchDto>> GetConfigurations(SearchData request)
    {
        _logger.LogDebug("Fetching configurations list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var configurations = await _dbContext.ConfigurationsSearch
           .AsNoTracking()
           .Select(ConfigurationSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} configurations", configurations.TotalCount);

        return configurations;
    }

}
