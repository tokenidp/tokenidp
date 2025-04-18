namespace Identity.Application.PowerBI;

public class PowerBIRequest : IRequest<PowerBIResponse>
{
    public IEnumerable<BIReport> Reports { get; set; }
}

public class BIEmbedTokenRequest
{
    public int LifetimeInMinutes { get; set; }

    public BIEmbedDataset[] Datasets { get; set; }
    public BIEmbedReport[] Reports { get; set; }

    public BIEmbedTokenRequest(int lifeTimeInMinutes)
    {
        LifetimeInMinutes = lifeTimeInMinutes;
    }

    public void CreateRequest(IEnumerable<BIReport> reports)
    {
        List<BIEmbedDataset> bIEmbedDatasets = new();
        List<BIEmbedReport> bIEmbedReports = new();

        foreach (var report in reports)
        {
            bIEmbedDatasets.Add(new(report.DatasetId));

            bIEmbedReports.Add(new(report.ReportId, false));
        }

        Datasets = bIEmbedDatasets.ToArray();
        Reports = bIEmbedReports.ToArray();
    }
}

public class BIEmbedDataset
{
    public string Id { get; set; }

    public BIEmbedDataset(string id)
    {
        Id = id;
    }
}

public class BIEmbedReport
{
    public string Id { get; set; }
    public bool AllowEdit { get; set; }

    public BIEmbedReport(string id, bool allowEdit)
    {
        Id = id;
        AllowEdit = allowEdit;
    }
}

public class PowerBIRequestHandler : IRequestHandler<PowerBIRequest, PowerBIResponse>
{
    private readonly IPowerBIService _powerBIService;
    private readonly ICurrentUserService _currentUserService;

    public PowerBIRequestHandler(IPowerBIService powerBIService,
                    ICurrentUserService currentUserService)
    {
        _powerBIService = powerBIService;
        _currentUserService = currentUserService;
    }

    public async Task<PowerBIResponse> Handle(PowerBIRequest request,
        CancellationToken cancellationToken)
    {
        var accessToken = await _powerBIService.GetADAccessToken();

        if (string.IsNullOrEmpty(accessToken))
        {
            return new(false, "AD credentials are not valid.");
        }

        var reportIds = _currentUserService.GetRoles();

        if (!reportIds.IsSafe())
        {
            return new(false, "The user doesn't have access to the reports.");
        }

        IEnumerable<BIReport> reports = default;

        if (request.Reports.IsSafe())
        {
            reports = request.Reports;
        }
        else
        {
            reports = await _powerBIService.GetReports(accessToken, reportIds);
        }

        BIEmbedTokenRequest embedTokenRequest = new(60);

        embedTokenRequest.CreateRequest(reports);

        var embedTokenReponse = await _powerBIService
            .GetEmbedToken(accessToken, embedTokenRequest);

        embedTokenReponse.SetResponse(reports);

        return embedTokenReponse;
    }
}
