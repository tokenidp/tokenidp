namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class LookupType
{
    [Key]
    public int Id { get; private set; }
    public int ParentId { get; private set; }
    public int TenantId { get; private set; }
    public string LookupTypeName { get; private set; }
    public virtual ICollection<LookupValue> LookupValues { get; private set; }

    /// <summary>
    /// Parameter less constructor is required for Entity Framework
    /// </summary>
    private LookupType()
    {
        LookupValues = new List<LookupValue>();
    }
}
