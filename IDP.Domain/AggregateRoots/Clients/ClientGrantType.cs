using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Clients;

public class ClientGrantType : BaseEntity
{
    public int ClientId { get; private set; }
    public GrantTypes AllowedGrantType { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientGrantType()
    {

    }
}
