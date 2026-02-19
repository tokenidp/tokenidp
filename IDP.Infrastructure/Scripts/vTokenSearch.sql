CREATE VIEW [dbo].[vTokenSearch]

AS
SELECT
    trm.Id,
    trm.SourceTokenId As TokenId,
    trm.TenantId, 
    trm.SourceType,
    trm.TokenType,
    c.ClientId,
    c.ClientName,
    trm.UserId,
    CONCAT(u.FirstName, ' ', u.LastName) as UserName,
    trm.IssuedAt,
    trm.ExpiresAt,
    trm.Status,
    trm.Scopes,
    trm.Audience,
    trm.IssuedByIp,
    trm.IssuedUserAgent,
    trm.IssuedBy,
    trm.RevokedAt,
    trm.RevokedReason,
    trm.RevokedBy,
    trm.RevokedByIp,
    trm.CreatedOn,
    trm.UpdatedOn
FROM dbo.TokenReadModel trm
INNER JOIN Clients c ON trm.ClientId = c.Id
LEFT JOIN Users u ON trm.UserId = u.Id

GO