namespace Identity.Application.Tenants.Queries;

public class GetTenantById : IRequest<TenantDto>
{
    public int Id { get; set; }
}

public class GetTenantByIdHandler : IRequestHandler<GetTenantById, TenantDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetTenantByIdHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<TenantDto> Handle(GetTenantById request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Tenants
            .Where(u => u.Id == request.Id)
            .ProjectTo<TenantDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return user;
    }
}