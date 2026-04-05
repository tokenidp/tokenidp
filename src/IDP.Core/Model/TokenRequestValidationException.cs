namespace IDP.Core.Model;

internal sealed class TokenRequestValidationException : Exception
{
    public string Error { get; }
    public int StatusCode { get; }

    public TokenRequestValidationException(string error, string description)
        : base(description)
    {
        Error = error;
        StatusCode = error == "invalid_client"
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status400BadRequest;
    }
}

internal static class TokenRequestValidationResultFactory
{
    public static IResult Create(TokenRequestValidationException exception)
    {
        var errorResult = ApiResult<ApiError>.Failure(
            ApiError.Failure(exception.Error, exception.Message));

        return Results.Json(errorResult, statusCode: exception.StatusCode);
    }
}