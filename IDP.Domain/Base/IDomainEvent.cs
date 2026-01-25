namespace IDP.Domain.Base;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}