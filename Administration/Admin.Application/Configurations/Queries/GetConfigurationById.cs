namespace Identity.Application.Configurations.Queries;

public class GetConfigurationById : IRequest<ConfigurationDto>
{
    public int Id { get; set; }
}

public class GetConfigurationByIdHandler : IRequestHandler<GetConfigurationById, ConfigurationDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetConfigurationByIdHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ConfigurationDto> Handle(GetConfigurationById request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.AppConfigurations
            .Where(u => u.Id == request.Id)
            .ProjectTo<ConfigurationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return user;
    }
}
