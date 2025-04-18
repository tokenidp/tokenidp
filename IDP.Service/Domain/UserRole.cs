namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class UserRole : IdentityUserRole<int>
{
    public int Id { get; private set; }
    public virtual Role Role { get; private set; }
    public virtual User User { get; private set; }

    public UserRole() : base() { }
}
