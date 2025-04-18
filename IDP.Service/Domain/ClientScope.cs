namespace IDP.Service.Domain;

public class ClientScope
{
   
    [Key]
    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public string Scope { get; private set; }
 
    public virtual Client Client { get; private set; }

    private ClientScope()
    {

    }
}
