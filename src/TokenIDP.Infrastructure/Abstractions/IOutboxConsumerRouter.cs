namespace TokenIDP.Infrastructure.Abstractions;

public interface IOutboxConsumerRouter
{
    IReadOnlyList<string> ResolveConsumers(IDomainEvent evt);
}
