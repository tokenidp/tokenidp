namespace Admin.Core.Roles;

internal class RoleLookup
{
    public RoleLookup(int id, string name)
    {
        Id = id;
        Name = name;
    }

    internal static Expression<Func<Role, RoleLookup>> Projection =>
    u => new RoleLookup(u.Id, u.Name);

    public int Id { get; private set; }
    public string Name { get; private set; }
}
