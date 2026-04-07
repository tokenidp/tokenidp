using TokenIDP.Domain.AggregateRoots.Outbox;

namespace TokenIDP.Infrastructure.Outbox.Abstractions;

public interface IOutboxMapperResolver
{
    OutboxEvent Resolve(IDomainEvent evt);
}

