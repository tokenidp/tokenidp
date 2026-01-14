namespace Admin.Core.Endpoints;

internal static class EndpointResultMapper
{
    public static IResult ToOkOrError<T>(ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result);
        }

        return ToErrorResult(result.Error);
    }

    public static IResult ToCreatedOrError<T>(ApiResult<T> result, string location)
    {
        if (result.IsSuccess)
        {
            return Results.Created(location, result);
        }

        return ToErrorResult(result.Error);
    }

    public static IResult ToNoContentOrError<T>(ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return ToErrorResult(result.Error);
    }

    private static IResult ToErrorResult(ApiError? error)
    {
        var apiError = error ?? ApiError.Failure("An unexpected error occurred.");
        var payload = ApiResult<ApiError>.Failure(apiError);

        return string.Equals(apiError.Code, "NotFound", StringComparison.OrdinalIgnoreCase)
            ? Results.NotFound(payload)
            : Results.BadRequest(payload);
    }
}
