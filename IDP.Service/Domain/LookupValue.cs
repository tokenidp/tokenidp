namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class LookupValue
{
    [Key]
    public int Id { get; private set; }
    public int LookupTypeId { get; private set; }
    public string LookupCode { get; private set; }
    public string Value { get; private set; }
    public bool? IsDeleted { get; private set; }
    public virtual LookupType LookupType { get; private set; }

    /// <summary>
    /// Parameter less constructor is required for Entity Framework
    /// </summary>
    private LookupValue() { }

}
