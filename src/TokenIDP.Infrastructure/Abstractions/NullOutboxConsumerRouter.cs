namespace TokenIDP.Infrastructure.Abstractions;

internal class NullOutboxConsumerRouter : IOutboxConsumerRouter
{

    public static readonly NullOutboxConsumerRouter Instance = new();

    public IReadOnlyList<string> ResolveConsumers(IDomainEvent evt)
    {
        throw new NotImplementedException();
    }
}

