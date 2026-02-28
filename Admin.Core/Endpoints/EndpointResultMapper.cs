namespace Admin.Core.Endpoints;

internal static class EndpointResultMapper
{
    public static IResult ToOkOrError<T>(ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result);
        }

        return ToErrorResult(result);
    }

    public static IResult ToCreatedOrError<T>(ApiResult<T> result, string location)
    {
        if (result.IsSuccess)
        {
            return Results.Created(location, result);
        }

        return ToErrorResult(result);
    }

    public static IResult ToNoContentOrError<T>(ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return ToErrorResult(result);
    }

    private static IResult ToErrorResult<T>(ApiResult<T> result)
    {
        if (result.Error is null && (result.Errors is null || result.Errors.Count == 0))
        {
            result = ApiResult<T>.Failure(ApiError.Failure("An unexpected error occurred."));
        }

        var isNotFound =
            string.Equals(result.Error?.Code, "NotFound", StringComparison.OrdinalIgnoreCase) ||
            (result.Errors?.Any(e => string.Equals(e.Code, "NotFound", StringComparison.OrdinalIgnoreCase)) ?? false);

        return isNotFound
            ? Results.NotFound(result)
            : Results.BadRequest(result);
    }
}