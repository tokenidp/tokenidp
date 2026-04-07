namespace TokenIDP.Core.Abstractions;

public interface IApplicationEventDispatcher
{
    void Raise(IDomainEvent evt);
}

