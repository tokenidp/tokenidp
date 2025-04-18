namespace Identity.Application.Configurations.Queries;

public class GetConfigurations : PageInfo, IRequest<PaginatedList<ConfigurationSearchDto>>
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}

public class GetConfigurationsHandler : IRequestHandler<GetConfigurations, PaginatedList<ConfigurationSearchDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetConfigurationsHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<ConfigurationSearchDto>> Handle(GetConfigurations request, CancellationToken cancellationToken)
    {
        var users = await _dbContext.ConfigurationsSearch
           .AsNoTracking()
           .ProjectTo<ConfigurationSearchDto>(_mapper.ConfigurationProvider)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }
}
