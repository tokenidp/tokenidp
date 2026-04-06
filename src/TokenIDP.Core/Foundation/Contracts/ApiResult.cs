namespace TokenIDP.Core.Foundation.Contracts;

public sealed class ApiResult<TResult>
{
    public bool IsSuccess { get; private set; }
    public TResult? Value { get; private set; }
    public ApiError? Error { get; private set; }
    public IList<ApiError>? Errors { get; private set; }

    private ApiResult(TResult value)
    {
        IsSuccess = true;
        Value = value;
    }

    private ApiResult(ApiError error)
    {
        IsSuccess = false;
        Error = error;
    }

    private ApiResult(IList<ApiError> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    public static ApiResult<TResult> Success(TResult value) => new(value);

    public static ApiResult<TResult> Failure(ApiError error) => new(error);

    public static ApiResult<TResult> Failure(IList<ApiError> errors) => new(errors);
}
