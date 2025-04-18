using Identity.Application.Lookups.Queries;
using Identity.Application.Roles.Queries;

namespace Identity.Application.Users.Queries;

public class GetUserLookups : IRequest<UserLookups>
{

}

public class UserLookups
{
    public IEnumerable<RoleLookup> RolesLookup { get; set; }
    public IEnumerable<StateLookupDto> StatesLookup { get; set; }
}

public class GetUserLookupsHandler : IRequestHandler<GetUserLookups, UserLookups>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetUserLookupsHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserLookups> Handle(GetUserLookups request, CancellationToken cancellationToken)
    {
        UserLookups userLookups = new();

        userLookups.RolesLookup = await _dbContext.AppRoles
           .AsNoTracking()
           .ProjectTo<RoleLookup>(_mapper.ConfigurationProvider)
           .ToListAsync();

        userLookups.StatesLookup = await _dbContext.StateLookups
           .AsNoTracking()
           .ProjectTo<StateLookupDto>(_mapper.ConfigurationProvider)
           .ToListAsync();

        return userLookups;
    }
}