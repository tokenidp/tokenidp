using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Abstractions;

internal class NullOutboxMapperResolver : IOutboxMapperResolver
{
    public static readonly NullOutboxMapperResolver Instance = new();

    public OutboxEvent Resolve(IDomainEvent evt)
    {
        throw new NotImplementedException();
    }
}
