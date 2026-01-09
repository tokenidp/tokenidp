using System.Text.Json.Serialization;

namespace IDP.Core.Model;

internal class UserInfo
{
    [JsonPropertyName("sub")]
    public int UserId { get; private set; }
    [JsonPropertyName("name")]
    public string Name { get; private set; }
    [JsonPropertyName("email")]
    public string Email { get; private set; }
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; private set; }
    [JsonPropertyName("given_name")]
    public string GivenName { get; private set; }
    [JsonPropertyName("family_name")]
    public string FamilyName { get; private set; }
    [JsonPropertyName("preferred_username")]
    public string PreferredUserName { get; private set; }
    [JsonPropertyName("profile")]
    public string Profile { get; private set; }
    [JsonPropertyName("picture")]
    public string Picture { get; private set; }
    [JsonPropertyName("website")]
    public string Website { get; private set; }
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; private set; }
    [JsonPropertyName("phone_number_verified")]
    public bool PhoneNumberVerified { get; private set; }
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; private set; }

    private UserInfo() { }

    public static UserInfo FromUser(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return new UserInfo
        {
            UserId = user.Id,
            Name = user.FullName,
            Email = user.Email ?? string.Empty,
            EmailVerified = user.EmailConfirmed,
            GivenName = user.FirstName,
            FamilyName = user.LastName,
            PreferredUserName = user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            PhoneNumberVerified = user.PhoneNumberConfirmed,
            UpdatedAt = user.UpdatedOn ?? user.CreatedOn
        };
    }
}
