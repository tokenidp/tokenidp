namespace Identity.Service.Security;

public class DynamicPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public DynamicPermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
