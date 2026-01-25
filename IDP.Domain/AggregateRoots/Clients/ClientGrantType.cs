using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Clients;

public class ClientGrantType : Entity<int>
{
    public int ClientId { get; private set; }
    public GrantTypes AllowedGrantType { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientGrantType()
    {

    }

    private ClientGrantType(GrantTypes grantType)
    {
        AllowedGrantType = grantType;
    }

    public static Result Create(GrantTypes grantType, out ClientGrantType? clientGrantType)
    {
        clientGrantType = new ClientGrantType(grantType);
        return Result.Success(0);
    }
}