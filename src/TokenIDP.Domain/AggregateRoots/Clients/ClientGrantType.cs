namespace TokenIDP.Domain.AggregateRoots.Clients;

public enum GrantTypes
{
    authorization_code,
    refresh_token,
    client_credentials,
    device_code,
    ciba,
    password
}

public class ClientGrantType : Entity<int>
{
    public int ClientId { get; private set; }
    public GrantTypes AllowedGrantType { get; private set; }

    public virtual Client Client { get; private set; } = default!;

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
