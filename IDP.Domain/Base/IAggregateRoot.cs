namespace IDP.Domain.Base;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    public int CreatedBy { get; }
    public DateTime CreatedOn { get; }
    public int? UpdatedBy { get; }
    public DateTime? UpdatedOn { get; }

    void SetCreated(int userId);

    void SetUpdated(int userId);

    void AddDomainEvent(IDomainEvent domainEvent);

    void ClearDomainEvents();
}
