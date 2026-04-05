
CREATE VIEW [dbo].[vUserSearch]
 
AS  
      Select u.Id, 
	  CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
	  u.TenantId,
	  u.UserName,
	  u.StatusId As [Status],
	  u.PhoneNumber,
	  u.Email,  
	  CONCAT(ua.AddressLine1, ' ', ua.City, ', ', ua.[State], ' ', ua.PostalCode) AS FullAddress,
	  CASE WHEN up.Id is NULL THEN 'System' ELSE up.FirstName END FirstName,
	  CASE WHEN up.Id is NULL THEN 'Administrator' ELSE up.LastName END LastName,
	   Roles = STUFF((
			SELECT ', ' + r.RoleName
			FROM dbo.UserRoles ur
			INNER JOIN dbo.Roles r ON ur.RoleId = r.Id
			WHERE ur.UserId = u.Id
			FOR XML PATH(''), TYPE
		).value('.', 'nvarchar(500)'), 1, 2, '')
	  From dbo.Users u
	  LEFT JOIN dbo.Users up on up.Id = u.EffectiveUserId
	  Left JOIN UserAddresses ua on u.Id = ua.UserId

GO