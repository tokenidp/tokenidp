using IDP.Domain.AggregateRoots.Permissions;

namespace IDP.Domain.AggregateRoots.Clients;

public class ClientApiResource : Entity<int>
{
    public int ClientId { get; private set; }
    public int PermissionId { get; private set; }
    public virtual Client Client { get; private set; } = default!;
    public virtual Permission Permission { get; private set; } = default!;

    private ClientApiResource()
    {

    }
}
