using System.Security.Cryptography;
using System.Text;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.Foundation.Security;

public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string Prefix = "aesgcm:v1:";
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;
    private readonly string _keyId;

    public AesGcmSecretProtector(string keyBase64, string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("Secret encryption key is not configured.");
        }

        var key = Convert.FromBase64String(keyBase64);
        if (key.Length != KeySize)
        {
            throw new InvalidOperationException("Secret encryption key must be 32 bytes (AES-256).");
        }

        _key = key;
        _keyId = string.IsNullOrWhiteSpace(keyId) ? "default" : keyId.Trim();
    }

    public bool IsEncrypted(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    public string? Encrypt(string? plainText, string aadContext)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return plainText;
        }

        if (IsEncrypted(plainText))
        {
            return plainText;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[TagSize];
        var aad = Encoding.UTF8.GetBytes(aadContext ?? string.Empty);

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag, aad);

        CryptographicOperations.ZeroMemory(plainBytes);

        return $"{Prefix}{_keyId}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    public string? Decrypt(string? encryptedValue, string aadContext)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return encryptedValue;
        }

        if (!IsEncrypted(encryptedValue))
        {
            return encryptedValue;
        }

        var parts = encryptedValue.Split(':', StringSplitOptions.None);
        if (parts.Length != 6)
        {
            throw new CryptographicException("Encrypted value format is invalid.");
        }

        var nonce = Convert.FromBase64String(parts[3]);
        var tag = Convert.FromBase64String(parts[4]);
        var ciphertext = Convert.FromBase64String(parts[5]);
        var plaintext = new byte[ciphertext.Length];
        var aad = Encoding.UTF8.GetBytes(aadContext ?? string.Empty);

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);

        var result = Encoding.UTF8.GetString(plaintext);
        CryptographicOperations.ZeroMemory(plaintext);

        return result;
    }
}
