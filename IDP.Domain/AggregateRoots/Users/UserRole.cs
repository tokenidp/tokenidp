namespace IDP.Domain.AggregateRoots.Users;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class UserRole : BaseEntity
{
    public int RoleId { get; private set; }
    public int UserId { get; private set; }
    public virtual Role Role { get; private set; }
    public virtual User User { get; private set; }

    public UserRole() : base() { }

    public UserRole(int roleId)
    {
        RoleId = roleId;
    }
}
