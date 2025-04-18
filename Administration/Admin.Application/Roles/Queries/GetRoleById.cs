namespace Identity.Application.Roles.Queries;

public class GetRoleById : IRequest<RoleDto>
{
    public int Id { get; set; }
}

public class GetRoleByIdHandler : IRequestHandler<GetRoleById, RoleDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetRoleByIdHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<RoleDto> Handle(GetRoleById request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.AppRoles
            .Where(u => u.Id == request.Id)
            .ProjectTo<RoleDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return user;
    }
}