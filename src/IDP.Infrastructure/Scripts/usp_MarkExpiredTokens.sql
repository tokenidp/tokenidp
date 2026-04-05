CREATE   PROCEDURE [dbo].[usp_MarkExpiredTokens]
    @BatchSize INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH cte AS (
        SELECT TOP (@BatchSize) Id
        FROM dbo.Tokens WITH (READPAST, UPDLOCK, ROWLOCK)
        WHERE TokenStatus = 1
          AND ExpiresAt < SYSUTCDATETIME()
        ORDER BY ExpiresAt
    )
    UPDATE t
    SET TokenStatus = 3
    FROM dbo.Tokens t
    INNER JOIN cte ON cte.Id = t.Id;
END
GO