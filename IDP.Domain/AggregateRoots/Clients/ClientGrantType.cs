namespace IDP.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class ClientGrantType : BaseEntity
{
    public enum GrantTypes
    {
        authorization_code,
        refresh_token,
        client_credentials,
        device_code,
        ciba
    }

    public int ClientId { get; private set; }
    public GrantTypes AllowedGrantType { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientGrantType()
    {

    }
}
