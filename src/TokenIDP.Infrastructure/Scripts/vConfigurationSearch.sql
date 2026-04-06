CREATE VIEW [dbo].[vConfigurationSearch]
 
AS  
      Select r.Id, 	 
	  t.TenantName,
	  r.ConfigKey,
	  r.ConfigValue,
	  FirstName,
	  LastName,
	  r.IsEditable
	  From dbo.[Configurations] r
	  INNER JOIN dbo.Tenants t on t.Id = r.TenantId
	  INNER JOIN dbo.Users u on u.Id = r.EffectiveUserId
	  Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
GO