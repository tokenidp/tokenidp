namespace IDP.Foundation.Options;

public sealed class SecretEncryptionOptions
{
    public const string SectionName = "Security:SecretEncryption";

    public string KeyBase64 { get; set; } = string.Empty;

    public string KeyId { get; set; } = "default";
}