using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.Base;

namespace TokenIDP.Infrastructure.Outbox;

public interface IOutboxMapper
{
    bool CanHandle(IDomainEvent evt);
    OutboxEvent Map(IDomainEvent evt);
}
