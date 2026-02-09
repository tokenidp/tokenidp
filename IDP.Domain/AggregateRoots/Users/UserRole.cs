namespace IDP.Domain.AggregateRoots.Users;

public class UserRole : Entity<int>
{
    public int RoleId { get; private set; }
    public int UserId { get; private set; }
    public virtual Role Role { get; private set; } = default!;
    public virtual User User { get; private set; } = default!;

    public UserRole() : base() { }

    public UserRole(int roleId)
    {
        RoleId = roleId;
    }
}
