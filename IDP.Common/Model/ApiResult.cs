namespace IDP.Common.Model;

public class ApiResult<TResult>
{
    public bool IsSuccess { get; private set; }
    public TResult Value { get; private set; }

    private ApiResult(TResult value, bool isSuccess)
    {
        IsSuccess = isSuccess;
        Value = value;
    }

    public static ApiResult<TResult> Success(TResult value) => new(value, true);

    public static ApiResult<TResult> Failure(TResult error) => new(error, false);
}