namespace TokenIDP.Core.OAuth.Model;

internal sealed class BackchannelAuthenticationValidationException : Exception
{
    public string Error { get; }
    public int StatusCode { get; }

    public BackchannelAuthenticationValidationException(string error, string description)
        : base(description)
    {
        Error = error;
        StatusCode = error switch
        {
            "invalid_client" => StatusCodes.Status401Unauthorized,
            "access_denied" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
    }
}

internal static class BackchannelAuthenticationValidationResultFactory
{
    public static IResult Create(BackchannelAuthenticationValidationException exception)
    {
        var errorResult = ApiResult<ApiError>.Failure(
            ApiError.Failure(exception.Error, exception.Message));

        return Results.Json(errorResult, statusCode: exception.StatusCode);
    }
}
