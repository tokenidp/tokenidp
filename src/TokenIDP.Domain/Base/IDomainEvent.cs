namespace TokenIDP.Domain.Base;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
