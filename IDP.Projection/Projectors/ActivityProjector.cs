namespace IDP.Projection.Projectors;

internal class ActivityProjector
{
    private readonly IApplicationDbContext _db;
    private IAppLogger<ActivityProjector> _appLogger;

    public ActivityProjector(IApplicationDbContext db,
        IAppLogger<ActivityProjector> appLogger)
    {
        _db = db;
        _appLogger = appLogger;
    }
}
