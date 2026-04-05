namespace IDP.Core.Model;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";
    public int SessionTtlMinutes { get; set; } = 10;
}
