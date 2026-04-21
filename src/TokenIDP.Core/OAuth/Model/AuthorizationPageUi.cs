namespace TokenIDP.Core.OAuth.Model;

public sealed class AuthorizationPageUi
{
    public string ProductName { get; set; } = "TokenIDP";
    public string? LogoUrl { get; set; }
    public string? Theme { get; set; }
    public string? AccentColor { get; set; }
    public string? LoginText { get; set; }

    public bool AllowLocalLogin { get; set; } = true;
    public bool AllowStaySignedIn { get; set; } = true;

    public bool AllowSignup { get; set; } = true;
    public string SignupText { get; set; } = "Create account";
    public string SignupUrl { get; set; } = "/signup";

    public List<ExternalProviderUi> ExternalProviders { get; set; } = new();
}

public sealed class ExternalProviderUi
{
    public string DisplayName { get; init; } = default!;
    public bool Enabled { get; init; } = true;
}
