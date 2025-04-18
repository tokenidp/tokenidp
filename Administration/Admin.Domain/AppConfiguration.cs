using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class AppConfiguration : BaseEntity, ITenant, IAggregateRoot
{
    public int TenantId { get; private set; }
    public string ConfigKey { get; private set; }
    public string ConfigValue { get; private set; }
    public bool? IsDisplay { get; private set; }
    public bool? IsDeleted { get; private set; }
    public bool ShowToTenant { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    private AppConfiguration() { }

    public AppConfiguration(int tenantId,
        string configKey,
        string configValue,
        bool? isDisplay,
        bool isDefaultForTenant)
    {
        TenantId = tenantId;
        ConfigKey = configKey;
        ConfigValue = configValue;
        IsDisplay = isDisplay;
        ShowToTenant = isDefaultForTenant;
    }

    public void UpdateConfiguration(
        string configValue,
        bool? isDisplay,
        bool isDefaultForTenant)
    {
        ConfigValue = configValue;
        IsDisplay = isDisplay;
        ShowToTenant = isDefaultForTenant;
    }
}
