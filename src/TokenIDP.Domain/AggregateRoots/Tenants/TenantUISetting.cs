namespace TokenIDP.Domain.AggregateRoots.Tenants;

public class TenantUISetting : Entity<int>
{
    private TenantUISetting() { }

    public int TenantId { get; private set; }
    public string? Theme { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? DefaultLanguage { get; private set; }
    public string? LoginText { get; private set; }

    public virtual Tenant Tenant { get; private set; } = default!;

    public static TenantUISetting Create(
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText)
    {
        return new TenantUISetting
        {
            Theme = theme,
            LogoUrl = logo,
            PrimaryColor = primaryColor,
            DefaultLanguage = defaultLanguage,
            LoginText = loginText,
        };
    }

    public void Update(
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText)
    {
        Theme = theme;
        LogoUrl = logo;
        PrimaryColor = primaryColor;
        DefaultLanguage = defaultLanguage;
        LoginText = loginText;
    }
}

