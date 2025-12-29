namespace IDP.Domain.Base;

public class Result
{
    public int Id { get; private set; }

    public Dictionary<string, string> Errors { get; private set; }

    public Result(int id)
    {
        Id = id;
    }

    public Result(Dictionary<string, string> errors)
    {
        Errors = errors;
    }

    public static Result Success(int id)
    {
        return new Result(id);
    }

    public static Result Failure(string code, string message)
    {
        var errors = new Dictionary<string, string>
        {
            { code, message }
        };

        return new Result(errors);
    }

    public static Result Failure(Dictionary<string, string> errors)
    {
        return new Result(errors);
    }
}
