namespace IDP.Core.Admin.Users;

internal class UserInfo
{
    public int UserId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public bool EmailVerified { get; private set; }
    public string GivenName { get; private set; }
    public string MiddleName { get; private set; }
    public string NickName { get; private set; }
    public string FamilyName { get; private set; }
    public string PreferredUserName { get; private set; }
    public string Profile { get; private set; }
    public string Picture { get; private set; }
    public string Website { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool PhoneNumberVerified { get; private set; }
    public DateTime Birthdate { get; private set; }
    public string ZoneInfo { get; private set; }
    public string Locale { get; private set; }
    public string Address { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
