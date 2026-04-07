namespace TokenIDP.Infrastructure.Outbox.Abstractions;

public interface IOutboxConsumerRouter
{
    IReadOnlyList<string> ResolveConsumers(IDomainEvent evt);
}
