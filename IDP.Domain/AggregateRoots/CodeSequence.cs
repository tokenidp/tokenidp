namespace IDP.Domain.AggregateRoots;

public class CodeSequence : ITenant
{
    public int TenantId { get; private set; }
    public string SequenceKey { get; private set; } = default!;
    public int LastValue { get; private set; }
}
