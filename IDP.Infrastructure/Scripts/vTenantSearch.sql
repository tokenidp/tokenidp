CREATE VIEW [dbo].[vTenantSearch]

AS  
      Select t.Id, 	 
	  t.TenantName,
	  t.TenantCode,
	  t.Email,
	  Case When ISNULL(t.IsActive, 1) = 1 then 'Yes' else 'No' end Active,
	  u.FirstName,
	  u.LastName	  
	  From dbo.Tenants t
	  INNER JOIN dbo.Users u on u.Id = t.EffectiveUserId
GO