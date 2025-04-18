namespace Identity.Application.Configurations.Commands;

public class UpdateConfiguration : IRequest<Result>
{
    public int Id { get; set; }
    public string ConfigValue { get; set; }
    public bool? IsDisplay { get; set; }
    public bool IsDefaultForTenant { get; set; }
}

public class UpdateConfigurationCommandHandler : IRequestHandler<UpdateConfiguration, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateConfigurationCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateConfiguration request, CancellationToken cancellationToken)
    {
        var configuration = await _dbContext.AppConfigurations.FirstOrDefaultAsync(c => c.Id == request.Id);

        if (configuration == null)
        {
            return Result.Failure("NotFound", "Configuration not found for the Id {0}".FormatString(request.Id));
        }

        configuration.UpdateConfiguration(
            request.ConfigValue,
            request.IsDisplay,
            request.IsDefaultForTenant);

        _dbContext.AppConfigurations.Update(configuration);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }
}