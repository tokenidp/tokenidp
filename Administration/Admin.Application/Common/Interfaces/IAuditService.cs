namespace Identity.Application.Common.Interfaces;

public interface IAuditService
{
    void CreateAuditLog(IApplicationDbContext context);
}
