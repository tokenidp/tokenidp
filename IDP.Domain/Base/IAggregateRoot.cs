namespace IDP.Domain.Base;

internal interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
