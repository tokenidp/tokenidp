namespace IDP.Domain.AggregateRoots.Clients;

public class ClientAudience : Entity<int>
{
    public int ClientId { get; private set; }
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public virtual Client Client { get; private set; } = default!;

    private ClientAudience()
    {

    }

    private ClientAudience(string name, bool isActive)
    {
        Name = name;
        IsActive = isActive;
    }

    public static Result Create(string name, bool isActive, out ClientAudience? clientAudience)
    {
        clientAudience = null;

        var validation = ValidateRequired(name, "client.audience.invalid",
            "Audience cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        clientAudience = new ClientAudience(name.Trim(), isActive);
        return Result.Success(0);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}