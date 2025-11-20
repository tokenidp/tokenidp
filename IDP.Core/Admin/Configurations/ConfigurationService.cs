namespace IDP.Core.Admin.Configurations;

internal class ConfigurationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ConfigurationService(ApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result> CreateConfiguration(CreateUpdateConfiguration request)
    {
        Configuration configuration = new(_currentUserService.TenantId,
            request.ConfigKey,
            request.ConfigValue,
            request.IsDisplay,
            request.IsEditable);

        _dbContext.Configurations.Add(configuration);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<Result> UpdateConfiguration(CreateUpdateConfiguration request)
    {
        var configuration = await _dbContext.Configurations.FirstOrDefaultAsync(c => c.Id == request.Id);

        if (configuration == null)
        {
            return Result.Failure("NotFound", "Configuration not found for the Id {0}".FormatString(request.Id));
        }

        configuration.UpdateConfiguration(
            request.ConfigValue,
            request.IsDisplay,
            request.IsEditable);

        _dbContext.Configurations.Update(configuration);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<ConfigurationDto> GerConfigurationById(int configId)
    {
        var configuration = await _dbContext.Configurations
            .Where(u => u.Id == configId)
            .Select(ConfigurationDto.Projection)
            .FirstOrDefaultAsync();

        return configuration;
    }

    public async Task<PaginatedList<ConfigurationSearchDto>> GetConfigurations(SearchData request)
    {
        var configurations = await _dbContext.ConfigurationsSearch
           .AsNoTracking()
           .Select(ConfigurationSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return configurations;
    }

}
