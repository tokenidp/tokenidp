using Admin.Core;
using Admin.Core.Application.Mappings;

namespace Identity.Application.Reports.Queries;

public class GetReports : PageInfo, IRequest<PaginatedList<ReportSearchDto>>
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}

public class GetReportsHandler : IRequestHandler<GetReports, PaginatedList<ReportSearchDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetReportsHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedList<ReportSearchDto>> Handle(GetReports request, CancellationToken cancellationToken)
    {
        var users = await _dbContext.ReportsSearch
           .AsNoTracking()
           .ProjectTo<ReportSearchDto>(_mapper.ConfigurationProvider)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }
}
