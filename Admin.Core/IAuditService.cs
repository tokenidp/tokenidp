namespace Admin.Core;

public interface IAuditService
{
    void CreateAuditLog(IApplicationDbContext context);
}
