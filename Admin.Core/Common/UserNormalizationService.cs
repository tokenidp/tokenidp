namespace Admin.Core.Common;

public sealed class UserNormalizationService
{
    private readonly ILookupNormalizer _normalizer;

    public UserNormalizationService(ILookupNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public void Normalize(User user)
    {
        user.GetType().GetProperty(nameof(User.NormalizedUserName))!
            .SetValue(user, _normalizer.NormalizeName(user.UserName));

        user.GetType().GetProperty(nameof(User.NormalizedEmail))!
            .SetValue(user, _normalizer.NormalizeEmail(user.Email));
    }
}