namespace Identity.Domain.Entities;

public class Store : BaseEntity, ITenant, IAggregateRoot
{
    public int TenantId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Address1 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Zip { get; private set; }

    private Store()
    {

    }
}
