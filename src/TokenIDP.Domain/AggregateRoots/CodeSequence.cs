namespace TokenIDP.Domain.AggregateRoots;

public class CodeSequence : ITenant
{
    private CodeSequence() { }

    public CodeSequence(int tenantId, string sequenceKey, int lastValue = 0)
    {
        TenantId = tenantId;
        SequenceKey = sequenceKey;
        LastValue = lastValue;
    }

    public int TenantId { get; private set; }
    public string SequenceKey { get; private set; } = default!;
    public int LastValue { get; private set; }
}
