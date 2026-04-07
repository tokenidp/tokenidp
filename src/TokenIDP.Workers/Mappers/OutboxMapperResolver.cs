using TokenIDP.Infrastructure.Outbox.Abstractions;

namespace TokenIDP.Workers.Mappers;

public sealed class OutboxMapperResolver : IOutboxMapperResolver
{
    private readonly IEnumerable<IOutboxMapper> _mappers;

    public OutboxMapperResolver(IEnumerable<IOutboxMapper> mappers)
    {
        _mappers = mappers;
    }

    public OutboxEvent Resolve(IDomainEvent evt)
    {
        var mapper = _mappers.FirstOrDefault(m => m.CanHandle(evt))
            ?? throw new InvalidOperationException(
        $"No outbox mapper for {evt.GetType().Name}");

        return mapper.Map(evt);
    }
}
