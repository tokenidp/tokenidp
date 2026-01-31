
namespace IDP.Domain.AggregateRoots.Roles;

public class Role : IdentityRole<int>, IAggregateRoot, ITenant
{
    private readonly List<RolePermission> _rolePermissions = new();
    private readonly List<UserRole> _userRoles = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public int TenantId { get; private set; }
    public string RoleDescription { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsEditable { get; private set; }
    public bool IsDeleted { get; private set; }

    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Role() : base() { }

    public Role(
       int tenantId,
       string name,
       string description,
       bool isActive)
    {
        TenantId = tenantId;
        Name = name;
        RoleDescription = description;
        IsActive = isActive;
        IsDeleted = false;
        IsEditable = true;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public Result Update(string name,
        string description,
        bool isActive)
    {
        var validation = ValidateEditable()
            .Combine(ValidateName(name));

        if (!validation.IsSuccess)
            return validation;

        Name = name;
        RoleDescription = description;
        IsActive = isActive;

        return Result.Success(id: Id);
    }

    public Result Delete()
    {
        var validation = ValidateEditable();

        if (!validation.IsSuccess)
            return validation;

        IsDeleted = true;
        IsActive = false;

        return Result.Success(id: Id);
    }

    public Result AddPermission(int tenantPermissionId, string permissionKey, bool isAllowed)
    {
        var validation = ValidateEditable()
            .Combine(ValidatePermissionKey(permissionKey));

        if (!validation.IsSuccess)
            return validation;

        if (_rolePermissions.Any(p => p.PermissionKey == permissionKey))
        {
            return Result.Failure(
                "permission.already_exists",
                $"Permission '{permissionKey}' already exists.");
        }

        _rolePermissions.Add(new RolePermission(0, tenantPermissionId, permissionKey, isAllowed));

        return Result.Success(id: Id);
    }

    public Result UpdatePermission(string permissionKey, bool isAllowed)
    {
        var validation = ValidateEditable()
            .Combine(ValidatePermissionKey(permissionKey));

        if (!validation.IsSuccess)
            return validation;

        var permission = _rolePermissions
            .SingleOrDefault(p => p.PermissionKey == permissionKey);

        if (permission is null)
        {
            return Result.Failure(
                "permission.not_found",
                $"Permission '{permissionKey}' not found.");
        }

        permission.Set(isAllowed);

        return Result.Success(id: Id);
    }

    public Result RemovePermission(string permissionKey)
    {
        var validation = ValidateEditable()
            .Combine(ValidatePermissionKey(permissionKey));

        if (!validation.IsSuccess)
            return validation;

        var permission = _rolePermissions
            .SingleOrDefault(p => p.PermissionKey == permissionKey);

        if (permission is null)
        {
            return Result.Failure(
                "permission.not_found",
                $"Permission '{permissionKey}' not found.");
        }

        _rolePermissions.Remove(permission);
        return Result.Success(id: Id);
    }

    private Result ValidateEditable()
    {
        if (IsDeleted)
        {
            return Result.Failure(
                "role.deleted",
                "The role is deleted and cannot be modified.");
        }

        if (!IsEditable)
        {
            return Result.Failure(
                "role.not_editable",
                "The role is not editable.");
        }

        return Result.Success(id: Id);
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                "role.name.invalid",
                "Role name cannot be empty.");
        }

        return Result.Success(id: 0);
    }

    private static Result ValidatePermissionKey(string permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return Result.Failure(
                "permission.key.invalid",
                "Permission key cannot be empty.");
        }

        return Result.Success(id: 0);
    }

    public void SetCreated(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdated(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
