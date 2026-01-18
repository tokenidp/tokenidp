namespace IDP.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class ClientAudience : BaseEntity
{
    public int ClientId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientAudience()
    {

    }
}