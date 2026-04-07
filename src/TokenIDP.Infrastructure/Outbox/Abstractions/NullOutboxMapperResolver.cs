using TokenIDP.Domain.AggregateRoots.Outbox;

namespace TokenIDP.Infrastructure.Outbox.Abstractions;

internal class NullOutboxMapperResolver : IOutboxMapperResolver
{
    public static readonly NullOutboxMapperResolver Instance = new();

    public OutboxEvent Resolve(IDomainEvent evt)
    {
        throw new NotImplementedException();
    }
}

