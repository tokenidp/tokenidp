namespace Services.Common.Model;

public class Result<TResult>
{
    public bool IsSuccess { get; private set; }
    public TResult Value { get; private set; }

    private Result(TResult value, bool isSuccess)
    {
        IsSuccess = isSuccess;
        Value = value;
    }

    public static Result<TResult> Success(TResult value) => new(value, true);

    public static Result<TResult> Failure(TResult error) => new(error, false);
}