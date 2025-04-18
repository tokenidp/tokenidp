namespace Identity.Application.Users.Queries;

public class GetUserById : IRequest<UserDto>
{
    public int Id { get; set; }
}

public class GetUserByIdHandler : IRequestHandler<GetUserById, UserDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetUserByIdHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetUserById request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.AppUsers
            .Where(u => u.Id == request.Id)
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return user;
    }
}
