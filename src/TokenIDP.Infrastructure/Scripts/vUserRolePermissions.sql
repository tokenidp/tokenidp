CREATE VIEW [dbo].[vUserRolePermissions]
 
AS 

SELECT distinct c.Id, c.[Sequence],
c.ParentId,
c.Permissionkey,
c.PermissionName,
rc.IsAllowed,
c.Icon,
c.AccessUrl,
r.RoleName,
ur.UserId,
c.ControlType
FROM   dbo.[Permissions] c 
INNER JOIN dbo.RolePermissions rc ON c.Id = rc.PermissionId 
INNER JOIN dbo.Roles r ON rc.RoleId = r.Id 
INNER JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL) AND r.IsActive = 1

GO
