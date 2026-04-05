namespace IDP.Foundation.Options;

public class TokenOption
{
    public string? Key { get; set; }
    public string? KeyPath { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? CertificateSubjectName { get; set; }
    public string? CertificateStoreName { get; set; }
    public string? CertificateStoreLocation { get; set; }
    public string? CertificateThumbprint { get; set; }
}
