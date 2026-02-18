namespace IDP.Domain.AggregateRoots.Permissions;

public sealed class Permission : AggregateRoot<int>, ITenant
{
    private static readonly System.Text.RegularExpressions.Regex PermissionKeyRegex =
        new("^[a-z0-9]+([._][a-z0-9]+)*$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly List<Permission> _children = new();

    public int TenantId { get; private set; }
    public int? ParentId { get; private set; }
    public int Sequence { get; private set; }

    public string PermissionKey { get; private set; } = default!;
    public string PermissionName { get; private set; } = default!;

    public string? AccessUrl { get; private set; }
    public string? Icon { get; private set; }
    public ControlTypes ControlType { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    public Tenant Tenant { get; private set; } = default!;

    public Permission? Parent { get; private set; }
    public IReadOnlyCollection<Permission> Children => _children.AsReadOnly();

    private Permission() { }

    public Permission(
        int tenantId,
        int? parentId,
        int sequence,
        string permissionKey,
        string permissionName,
        string? accessUrl,
        string? icon,
        string controlType,
        bool isActive)
    {
        var validation = ValidateKey(permissionKey)
            .Combine(ValidateName(permissionName))
            .Combine(ValidateControlType(controlType));

        if (!validation.IsSuccess)
            throw new InvalidOperationException(
                string.Join(", ", validation.Errors.Select(e => e.Message)));

        var controlTypeResult = ParseControlType(controlType);

        if (!controlTypeResult.IsSuccess)
            throw new InvalidOperationException(
                string.Join(", ", controlTypeResult.Errors.Select(e => e.Message)));

        Enum.TryParse<ControlTypes>(controlType, ignoreCase: true, out var parsedControlType);

        TenantId = tenantId;
        ParentId = parentId;
        Sequence = sequence;
        PermissionKey = permissionKey;
        PermissionName = permissionName;
        AccessUrl = accessUrl;
        Icon = icon;
        ControlType = parsedControlType;
        IsActive = isActive;
        IsSystem = false;
        SetCreated(1);
    }

    public Result Update(
        int? parentId,
        int sequence,
        string permissionKey,
        string permissionName,
        string? accessUrl,
        string? icon,
        string controlType,
        bool isActive)
    {
        var validation = ValidateKey(permissionKey)
            .Combine(ValidateName(permissionName))
            .Combine(ValidateControlType(controlType));

        if (!validation.IsSuccess)
            return validation;

        var controlTypeResult = ParseControlType(controlType);

        if (!controlTypeResult.IsSuccess)
            return controlTypeResult;

        Enum.TryParse<ControlTypes>(controlType, ignoreCase: true, out var parsedControlType);

        ParentId = parentId;
        Sequence = sequence;
        PermissionKey = permissionKey;
        PermissionName = permissionName;
        AccessUrl = accessUrl;
        Icon = icon;
        ControlType = parsedControlType;
        IsActive = isActive;

        return Result.Success(Id);
    }

    public Result ChangeOrder(int newSequence)
    {
        Sequence = newSequence;
        return Result.Success(Id);
    }

    private static Result ValidateKey(string key)
    {
        var trimmed = key?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Result.Failure(
                "permission.key.invalid",
                "Permission key cannot be empty.");
        }

        if (!PermissionKeyRegex.IsMatch(trimmed))
        {
            return Result.Failure(
                "permission.key.invalid",
                "Permission key format is invalid.");
        }

        return Result.Success(0);
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                "permission.name.invalid",
                "Permission name cannot be empty.");
        }

        return Result.Success(0);
    }

    private static Result ValidateControlType(string controlType)
    {
        if (string.IsNullOrWhiteSpace(controlType))
        {
            return Result.Failure(
                "permission.control_type.invalid",
                "Control type cannot be empty.");
        }

        return Result.Success(0);
    }

    private static Result ParseControlType(string controlType)
    {
        if (string.IsNullOrWhiteSpace(controlType))
        {
            return Result.Failure(
                "permission.control_type.empty",
                "ControlType cannot be empty.");
        }

        if (!Enum.TryParse<ControlTypes>(
                controlType,
                ignoreCase: true,
                out var parsed))
        {
            return Result.Failure(
                "permission.control_type.invalid",
                $"Invalid ControlType '{controlType}'.");
        }

        return Result.Success(0);
    }

    public static Result ValidateInput(
        string permissionKey,
        string permissionName,
        string controlType)
    {
        var validation = ValidateKey(permissionKey)
            .Combine(ValidateName(permissionName))
            .Combine(ValidateControlType(controlType));

        if (!validation.IsSuccess)
            return validation;

        return ParseControlType(controlType);
    }

    public void AddChild(Permission child)
    {
        _children.Add(child);
    }
}