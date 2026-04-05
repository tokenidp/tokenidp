namespace IDP.Domain.Base;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}