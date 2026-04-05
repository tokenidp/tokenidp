using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Abstractions;

public interface IOutboxMapperResolver
{
    OutboxEvent Resolve(IDomainEvent evt);
}
