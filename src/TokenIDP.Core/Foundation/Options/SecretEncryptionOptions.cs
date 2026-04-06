namespace TokenIDP.Core.Foundation.Options;

public sealed class SecretEncryptionOptions
{
    public string KeyBase64 { get; set; } = string.Empty;
    public string KeyId { get; set; } = "default";
}
