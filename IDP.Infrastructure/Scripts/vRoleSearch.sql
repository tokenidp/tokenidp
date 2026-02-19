CREATE VIEW [dbo].[vRoleSearch]
 
AS  
      Select r.Id, 	
	  t.TenantName,
	  r.RoleName,
	  CASE WHEN COALESCE(r.IsActive, 1) = 1 THEN 'Yes' ELSE 'No' END AS Active,
	  u.FirstName,
	  u.LastName
	  From dbo.Roles r
	  Inner Join dbo.Tenants t on t.Id = r.TenantId
	  INNER JOIN dbo.Users u on u.Id = r.EffectiveUserId
GO