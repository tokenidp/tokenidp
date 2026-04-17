CREATE VIEW [dbo].[vUserRolePermissions]
 
AS 

SELECT
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    CAST(MAX(CASE WHEN rc.IsAllowed = 1 THEN 1 ELSE 0 END) AS bit) AS IsAllowed,
    c.Icon,
    c.AccessUrl,
    MIN(r.RoleName) AS RoleName,
    ur.UserId,
    c.ControlType
FROM   dbo.[Permissions] c 
INNER JOIN dbo.RolePermissions rc ON c.Id = rc.PermissionId 
INNER JOIN dbo.Roles r ON rc.RoleId = r.Id 
INNER JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
Where (c.IsDeleted = 0 OR c.IsDeleted IS NULL)
  AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
  AND r.IsActive = 1
GROUP BY
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    c.Icon,
    c.AccessUrl,
    ur.UserId,
    c.ControlType

GO
