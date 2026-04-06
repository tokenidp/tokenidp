namespace TokenIDP.Core.Foundation.Abstractions;

public interface IApplicationEventDispatcher
{
    void Raise(IDomainEvent evt);
}

