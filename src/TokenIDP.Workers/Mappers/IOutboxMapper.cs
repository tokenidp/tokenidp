namespace TokenIDP.Workers.Mappers;

public interface IOutboxMapper
{
    bool CanHandle(IDomainEvent evt);
    OutboxEvent Map(IDomainEvent evt);
}
