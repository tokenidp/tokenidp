namespace Identity.Application.Configurations.Commands;

public class CreateConfiguration : IRequest<Result>
{
    public string ConfigKey { get; set; }
    public string ConfigValue { get; set; }
    public bool? IsDisplay { get; set; }
    public bool IsDefaultForTenant { get; set; }
}

public class CreateConfigurationCommandHandler : IRequestHandler<CreateConfiguration, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateConfigurationCommandHandler(IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CreateConfiguration request, CancellationToken cancellationToken)
    {
        AppConfiguration configuration = new(_currentUserService.TenantId,
            request.ConfigKey,
            request.ConfigValue,
            request.IsDisplay,
            request.IsDefaultForTenant);

        _dbContext.AppConfigurations.Add(configuration);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }
}
