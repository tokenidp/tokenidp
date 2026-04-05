namespace IDP.Foundation.Abstractions;

public interface IApplicationEventDispatcher
{
    void Raise(IDomainEvent evt);
}
