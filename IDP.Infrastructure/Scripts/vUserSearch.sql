
CREATE VIEW [dbo].[vUserSearch]
 
AS  
      Select u.Id, 
	  CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
	  u.TenantId,
	  u.UserName,
	  t.TenantName,
	  u.StatusId As [Status],
	  u.PhoneNumber,
	  u.Email,  
	  CONCAT(ua.AddressLine1, ' ', ua.City, ', ', ua.[State], ' ', ua.PostalCode) AS FullAddress,
	  up.FirstName,
	  up.LastName,
	   Roles = STUFF((
			SELECT ', ' + r.RoleName
			FROM dbo.UserRoles ur
			INNER JOIN dbo.Roles r ON ur.RoleId = r.Id
			WHERE ur.UserId = u.Id
			FOR XML PATH(''), TYPE
		).value('.', 'nvarchar(500)'), 1, 2, '')
	  From dbo.Users u
	  INNER JOIN dbo.Tenants t on t.Id = u.TenantId
	  INNER JOIN dbo.Users up on up.Id = u.EffectiveUserId
	  Left JOIN UserAddresses ua on u.Id = ua.UserId

GO