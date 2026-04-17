CREATE VIEW [dbo].[vConfigurationSearch]
 
AS  
      Select r.Id, 	 
	  t.TenantName,
	  r.ConfigKey,
	  r.ConfigValue,
	  CASE WHEN u.Id is NULL THEN 'System' ELSE u.FirstName END FirstName,
	  CASE WHEN u.Id is NULL THEN 'Administrator' ELSE u.LastName END LastName,
	  r.IsEditable
	  From dbo.[Configurations] r
	  INNER JOIN dbo.Tenants t on t.Id = r.TenantId
		AND (t.IsDeleted = 0 OR t.IsDeleted IS NULL)
	  LEFT JOIN dbo.Users u on u.Id = r.EffectiveUserId
		AND (u.IsDeleted = 0 OR u.IsDeleted IS NULL)
	  Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
GO
