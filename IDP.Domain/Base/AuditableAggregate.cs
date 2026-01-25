namespace IDP.Domain.Base;

public abstract class AuditableAggregate<TId> : AggregateRoot<TId>, IAuditableAggregate
{
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public void SetCreated(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdated(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
