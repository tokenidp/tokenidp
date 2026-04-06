namespace TokenIDP.Infrastructure.Emails.Primitives;

public sealed record SendEmailResult
(
    bool Success,
    bool PermanentFailure,
    string? ProviderMessageId,
    string? Error
)
{
    public static SendEmailResult Ok(string? providerMessageId = null)
        => new(true, false, providerMessageId, null);

    public static SendEmailResult TransientFailure(string error)
        => new(false, false, null, error);

    public static SendEmailResult PermanentFailureResult(string error)
        => new(false, true, null, error);
}

