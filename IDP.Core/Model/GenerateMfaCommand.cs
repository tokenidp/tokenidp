namespace IDP.Core.Model;

public sealed class GenerateMfaCommand
{
    public int UserId { get; init; }
    public string? ClientId { get; init; }
    public string? RedirectUri { get; init; }
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Scopes { get; init; }
}