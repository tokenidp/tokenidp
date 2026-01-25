namespace IDP.Domain.AggregateRoots.Lookups;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class LookupValue : Entity<int>
{
    public int LookupTypeId { get; private set; }
    public string LookupCode { get; private set; }
    public string Value { get; private set; }
    public string? LookupDescription { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsCodeEditable { get; private set; }
    public virtual LookupType LookupType { get; private set; }

    /// <summary>
    /// Parameter less constructor is required for Entity Framework
    /// </summary>
    private LookupValue() { }
}
