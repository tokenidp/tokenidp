namespace IDP.Core.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
internal class ClientSecret : BaseEntity
{
    public int ClientId { get; private set; }
    public string SecretHash { get; private set; }
    public string Description { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool? IsRevoked { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientSecret()
    {

    }
}
