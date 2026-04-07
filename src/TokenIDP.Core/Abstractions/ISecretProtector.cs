namespace TokenIDP.Core.Abstractions;

public interface ISecretProtector
{
    bool IsEncrypted(string? value);

    string? Encrypt(string? plainText, string aadContext);

    string? Decrypt(string? encryptedValue, string aadContext);
}
