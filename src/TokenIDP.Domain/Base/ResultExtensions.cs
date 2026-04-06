namespace TokenIDP.Domain.Base;

public static class ResultExtensions
{
    public static Result Combine(this Result first, Result second)
    {
        if (first.IsSuccess && second.IsSuccess)
            return Result.Success(first.Id);

        var errors = first.Errors
            .Concat(second.Errors)
            .Where(e => !e.IsNone);

        return Result.Failure(errors);
    }
}

