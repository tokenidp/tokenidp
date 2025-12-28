namespace IDP.Core.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
internal class ClientGrantType : BaseEntity
{
    public enum GrantType
    {
        authorization_code,
        refresh_token,
        client_credentials,
        device_code,
        ciba
    }

    public int ClientId { get; private set; }
    public GrantType AllowedGrantType { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientGrantType()
    {

    }
}
