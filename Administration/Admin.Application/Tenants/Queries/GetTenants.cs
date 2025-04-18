namespace Identity.Application.Tenants.Queries;

public class GetTenants : PageInfo, IRequest<PaginatedList<TenantSearchDto>>
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}

public class GetTenantsHandler : IRequestHandler<GetTenants, PaginatedList<TenantSearchDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetTenantsHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<TenantSearchDto>> Handle(GetTenants request, CancellationToken cancellationToken)
    {
        var users = await _dbContext.TenantsSearch
           .AsNoTracking()
           .ProjectTo<TenantSearchDto>(_mapper.ConfigurationProvider)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }
}

