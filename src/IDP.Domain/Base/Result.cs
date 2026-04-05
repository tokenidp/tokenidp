namespace IDP.Domain.Base;

public class Result
{
    public int Id { get; }
    public IReadOnlyCollection<DomainError> Errors { get; }

    private Result(int id)
    {
        Id = id;
        Errors = Array.Empty<DomainError>();
    }

    private Result(IEnumerable<DomainError> errors)
    {
        Errors = errors.ToList();
    }

    public bool IsSuccess => !Errors.Any();

    public static Result Success(int id) => new(id);

    public static Result Failure(string code, string message) =>
        new(new[] { new DomainError(code, message) });

    public static Result Failure(IEnumerable<DomainError> errors) =>
        new(errors);
}

public sealed record DomainError(string Code, string Message)
{
    public static DomainError None => new(string.Empty, string.Empty);

    public bool IsNone => string.IsNullOrWhiteSpace(Code);
}

