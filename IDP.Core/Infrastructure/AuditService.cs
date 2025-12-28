namespace IDP.Core.Infrastructure;

internal class AuditService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly JsonHelper _jsonHelper;

    public AuditService(
        ICurrentUserService currentUserService,
        JsonHelper jsonHelper)
    {
        _currentUserService = currentUserService;
        _jsonHelper = jsonHelper;
    }

    public void CreateAuditLog(ApplicationDbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .ToList();

        var serializeSettings = new System.Text.Json.JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        foreach (var entry in entries)
        {
            AuditLog auditLog = new
            (
                $"AdminPortal_{entry.Entity.GetType().Name}",
                entry.State.ToString(),
                entry.Properties.First(p => p.Metadata.IsPrimaryKey()).CurrentValue.ToString(),
                entry.State == EntityState.Modified ? _jsonHelper.SerializeObject(entry.OriginalValues, serializeSettings) : null,
                entry.State == EntityState.Deleted ? null : _jsonHelper.SerializeObject(entry.CurrentValues, serializeSettings)
            );

            auditLog.SetCreatedByAndCreatedOn(_currentUserService.UserId);

            context.AuditLogs.Add(auditLog);
        }
    }
}
