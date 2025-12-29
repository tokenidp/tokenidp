namespace IDP.Domain.AggregateRoots.Clients;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class ClientScope : BaseEntity
{
    public int ClientId { get; private set; }
    public string Scope { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientScope()
    {

    }
}
