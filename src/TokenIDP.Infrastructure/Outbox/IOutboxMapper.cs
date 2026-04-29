using TokenIDP.Domain.AggregateRoots.Outbox;

namespace TokenIDP.Infrastructure.Outbox;

public interface IOutboxMapper
{
    bool CanHandle(IDomainEvent evt);
    OutboxEvent Map(IDomainEvent evt);
}
