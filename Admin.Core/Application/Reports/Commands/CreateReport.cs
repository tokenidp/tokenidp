using Admin.Core;

namespace Identity.Application.Reports.Commands;

public class CreateReport : IRequest<Result>
{
    public int ParentId { get; set; }
    public string ReportKey { get; set; }
    public string ReportName { get; set; }
    public string AccessUrl { get; set; }
    public string ControlType { get; set; }
    public string ReportId { get; set; }
    public bool IsDefaultReport { get; set; }
    public bool ShowToTenant { get; set; }
    public bool IsActive { get; set; }
}

[SuppressMessage("SonarLint", "S4487", Justification = "_currentUserService will use in future")]
public class CreateReportCommandHandler : IRequestHandler<CreateReport, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateReportCommandHandler(IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CreateReport request, CancellationToken cancellationToken)
    {
        AppClaim report = new(request.ParentId,
            request.ReportKey,
            request.ReportName,
            request.AccessUrl,
            request.ControlType,
            request.ShowToTenant,
            request.IsActive);

        _dbContext.AppClaims.Add(report);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }
}
