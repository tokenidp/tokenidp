namespace Identity.Application.Users.Queries;

public class GetUsers : PageInfo, IRequest<PaginatedList<UserSearchDto>>
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}

public class GetUsersHandler : IRequestHandler<GetUsers, PaginatedList<UserSearchDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetUsersHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<UserSearchDto>> Handle(GetUsers request, CancellationToken cancellationToken)
    {
        var users = await _dbContext.UsersSearch
           .AsNoTracking()
           .ProjectTo<UserSearchDto>(_mapper.ConfigurationProvider)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }
}
