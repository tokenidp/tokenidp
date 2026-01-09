namespace Admin.Core;

internal static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result, int id)
    {
        if (result.Succeeded)
            return Result.Success(id);

        var errors = result.Errors
            .Select(e => new DomainError(e.Code, e.Description));

        return Result.Failure(errors);
    }
}