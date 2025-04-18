namespace Identity.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result, int id)
    {
        return result.Succeeded
            ? Result.Success(id)
            : Result.Failure(result.Errors.ToDictionary(e => e.Code, e => e.Description));
    }
}