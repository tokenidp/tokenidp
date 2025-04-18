namespace Services.Common.Model;

public class Result<TResult>
{
    public bool IsSuccess { get; private set; }
    public TResult Value { get; private set; }
    public string ErrorMessage { get; private set; }

    private Result(TResult value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public static Result<TResult> Success(TResult value) => new(value);

    public static Result<TResult> Failure(string error) => new(error);
}
