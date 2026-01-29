using IDP.Domain.AggregateRoots;

namespace IDP.Foundation.Abstractions;

public interface ITokenReadModelStore
{
    Task ProjectAsync(OutboxEvent evt, CancellationToken ct);
}
