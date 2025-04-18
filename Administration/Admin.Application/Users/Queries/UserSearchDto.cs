namespace Identity.Application.Users.Queries;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and used for automapper")]
public class UserSearchDto : IMapFrom<UserSearch>
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
    public string UpdatedBy { get; set; }
}
