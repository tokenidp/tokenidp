using Admin.Core;

namespace Identity.Application.Reports.Queries;

public class GetReportById : IRequest<ReportDto>
{
    public int Id { get; set; }
}

public class GetReportByIdHandler : IRequestHandler<GetReportById, ReportDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetReportByIdHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ReportDto> Handle(GetReportById request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.AppClaims
            .Where(u => u.Id == request.Id)
            .ProjectTo<ReportDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return user;
    }
}
