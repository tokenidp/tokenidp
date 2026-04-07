CREATE VIEW [dbo].[vRoleSearch]
 
AS  
      Select r.Id,
	  r.TenantId,
	  r.RoleName,
	  CASE WHEN COALESCE(r.IsActive, 1) = 1 THEN 'Yes' ELSE 'No' END AS Active,
	  CASE WHEN u.Id is NULL THEN 'System' ELSE u.FirstName END FirstName,
	  CASE WHEN u.Id is NULL THEN 'Administrator' ELSE u.LastName END LastName
	  From dbo.Roles r
	  LEFT JOIN dbo.Users u on u.Id = r.EffectiveUserId
GO