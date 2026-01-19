namespace IDP.Domain.AggregateRoots.Clients;

public class ClientSecret : BaseEntity
{
    public int ClientId { get; private set; }
    public string SecretHash { get; private set; }
    public string Description { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public virtual Client Client { get; private set; }

    private ClientSecret()
    {

    }

    private ClientSecret(string secretHash, string description, DateTime expiresAt)
    {
        SecretHash = secretHash;
        Description = description;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public static Result Create(
        string secretHash,
        string? description,
        DateTime? expiresAt,
        out ClientSecret? clientSecret)
    {
        clientSecret = null;

        var validation = ValidateRequired(secretHash, "client.secret.invalid",
            "Client secret cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var resolvedExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(10);
        clientSecret = new ClientSecret(
            secretHash.Trim(),
            (description ?? string.Empty).Trim(),
            resolvedExpiresAt);

        return Result.Success(0);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}