namespace IDP.Foundation.Options;

public class BootstrapOption
{
    public string RedirectUri { get; set; } = default!;
    public string LogoutRedirectUri { get; set; } = default!;
    public string AdminTempPassword { get; set; } = default!;
    public string AdminName { get; set; } = default!;
    public bool Enable { get; set; }
}