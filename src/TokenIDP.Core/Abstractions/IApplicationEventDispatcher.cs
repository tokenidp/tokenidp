namespace TokenIDP.Core.Abstractions;

public interface IApplicationEventDispatcher
{
    void Raise(IDomainEvent evt);
    Task RaiseAsync(IDomainEvent evt, CancellationToken cancellationToken = default);
}

