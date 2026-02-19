namespace IDP.Domain.AggregateRoots.Users;

public class UserCodeSequence : ITenant
{
    public int TenantId { get; private set; }
    public int LastValue { get; private set; }
}
