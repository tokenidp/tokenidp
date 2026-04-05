namespace IDP.Infrastructure.Abstractions;

public interface IOutboxConsumerRouter
{
    IReadOnlyList<string> ResolveConsumers(IDomainEvent evt);
}