namespace IDP.Core.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
internal partial class UserSearch
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string FullName { get; private set; }
    public string UserName { get; private set; }
    public string TenantName { get; private set; }
    public string Status { get; private set; }
    public string FullAddress { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Email { get; private set; }
    public string Roles { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private UserSearch()
    {

    }
}

internal partial class UserSearch
{
    public string UpdatedBy
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}

