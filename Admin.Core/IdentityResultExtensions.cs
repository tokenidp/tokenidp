namespace Admin.Core;

internal static class IdentityResultExtensions
{
    public static ApiResult<int> ToApiResult(this IdentityResult result, int id)
    {
        if (result.Succeeded)
            return ApiResult<int>.Success(id);

        var errors = result.Errors
            .ToDictionary(e => e.Code, e => e.Description);

        var apiError = ApiError.Failure(errors, string.Empty, "Identity validation failed.");

        return ApiResult<int>.Failure(apiError);
    }
}
