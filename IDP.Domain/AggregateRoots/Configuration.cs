namespace IDP.Domain.AggregateRoots;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class Configuration : BaseEntity, ITenant, IAggregateRoot
{
    public int TenantId { get; private set; }
    public string ConfigKey { get; private set; }
    public string ConfigValue { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsEditable { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    private Configuration() { }

    public Configuration(int tenantId,
        string configKey,
        string configValue,
        bool isEditable)
    {
        TenantId = tenantId;
        ConfigKey = configKey;
        ConfigValue = configValue;
        IsEditable = isEditable;
    }

    public void UpdateConfiguration(
        string configValue,
        bool? isDisplay,
        bool isEditable)
    {
        ConfigValue = configValue;
        IsEditable = isEditable;
    }
}
