using TokenIDP.Domain.AggregateRoots.Outbox;

namespace TokenIDP.Infrastructure.Abstractions;

public interface IOutboxMapperResolver
{
    OutboxEvent Resolve(IDomainEvent evt);
}

