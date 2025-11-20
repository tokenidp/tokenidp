using Admin.Core;

namespace Identity.Application.Reports.Commands;

public class UpdateReport : IRequest<Result>
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string ReportKey { get; set; }
    public string ReportName { get; set; }
    public string ReportId { get; set; }
    public bool IsDefaultReport { get; set; }
    public bool ShowToTenant { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateReportCommandHandler : IRequestHandler<UpdateReport, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateReportCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateReport request, CancellationToken cancellationToken)
    {
        var appClaim = await _dbContext.AppClaims.FirstOrDefaultAsync(c => c.Id == request.Id);

        if (appClaim == null)
        {
            return Result.Failure("NotFound", "Report not found for the Id {0}".FormatString(request.Id));
        }

        appClaim.UpdateAppClaim(
            request.ParentId,
            request.ReportKey,
            request.ReportName,
            request.ShowToTenant,
            request.IsActive
            );

        _dbContext.AppClaims.Update(appClaim);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }
}