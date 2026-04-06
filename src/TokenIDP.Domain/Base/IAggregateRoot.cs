namespace TokenIDP.Domain.Base;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    public int CreatedBy { get; }
    public DateTime CreatedAtUtc { get; }
    public int? UpdatedBy { get; }
    public DateTime? UpdatedAtUtc { get; }

    void SetCreated(int userId);

    void SetUpdated(int userId);

    void AddDomainEvent(IDomainEvent domainEvent);

    void ClearDomainEvents();
}

