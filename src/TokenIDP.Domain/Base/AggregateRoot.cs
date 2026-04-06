namespace TokenIDP.Domain.Base;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public int CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreated(int userId)
    {
        CreatedBy = userId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetUpdated(int userId)
    {
        UpdatedBy = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

