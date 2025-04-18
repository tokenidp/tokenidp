namespace Identity.Application.Roles.Queries;

public class GetRoles : PageInfo, IRequest<PaginatedList<RoleSearchDto>>
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}

public class GetRolesHandler : IRequestHandler<GetRoles, PaginatedList<RoleSearchDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetRolesHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<RoleSearchDto>> Handle(GetRoles request, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.RolesSearch
           .AsNoTracking()
           .ProjectTo<RoleSearchDto>(_mapper.ConfigurationProvider)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return roles;
    }
}