namespace IDP.Domain.AggregateRoots.Clients;

public class ClientApiResource : Entity<int>
{
    public int ClientId { get; private set; }
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public virtual Client Client { get; private set; } = default!;

    private ClientApiResource()
    {
    }

    private ClientApiResource(string name, bool isActive)
    {
        Name = name;
        IsActive = isActive;
    }

    public static Result Create(string name, bool isActive, out ClientApiResource? clientApiResource)
    {
        clientApiResource = null;

        var validation = ValidateRequired(name, "client.api_resource.invalid",
            "Api resource cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        clientApiResource = new ClientApiResource(name.Trim(), isActive);
        return Result.Success(0);
    }

    public Result Rename(string name)
    {
        var validation = ValidateRequired(name, "client.api_resource.invalid",
            "Api resource cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Name = name.Trim();
        return Result.Success(Id);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}