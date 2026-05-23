namespace TokenIDP.Core.Foundation.Options;

public sealed class CibaOptions
{
    public const string SectionName = "Ciba";

    public bool RequireNotificationDelivery { get; set; }
    public string? ApprovalBaseUrl { get; set; }
    public int ApprovalTokenBytes { get; set; } = 32;
    public int ApprovalTokenLifetimeSeconds { get; set; } = 300;
}
