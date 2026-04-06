namespace TokenIDP.Domain.AggregateRoots.Clients;

public class ClientScope : Entity<int>
{
    public int ClientId { get; private set; }
    public string Scope { get; private set; } = default!;

    public virtual Client Client { get; private set; } = default!;

    private ClientScope()
    {

    }

    private ClientScope(string scope)
    {
        Scope = scope;
    }

    public static Result Create(string scope, out ClientScope? clientScope)
    {
        clientScope = null;

        var validation = ValidateRequired(scope, "client.scope.invalid",
            "Scope cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        clientScope = new ClientScope(scope.Trim());
        return Result.Success(0);
    }

    public Result Rename(string scope)
    {
        var validation = ValidateRequired(scope, "client.scope.invalid",
            "Scope cannot be empty.");
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Scope = scope.Trim();
        return Result.Success(Id);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}
