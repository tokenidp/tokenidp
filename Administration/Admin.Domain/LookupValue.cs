using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class LookupValue : BaseEntity
{

    public int LookupTypeId { get; private set; }
    public string Code { get; private set; }
    public string Value { get; private set; }
    public string Description { get; private set; }
    public string UseFor { get; private set; }
    public bool? IsDefault { get; private set; }
    public bool? IsDeleted { get; private set; }
    public virtual LookupType LookupType { get; private set; }

    /// <summary>
    /// Parameter less constructor is required for Entity Framework
    /// </summary>
    private LookupValue() { }

}
