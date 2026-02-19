CREATE   PROCEDURE [dbo].[usp_PurgeOldTokens]
    @RetentionDays INT = 90,
    @BatchSize INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIME2(3) = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());

    ;WITH cte AS (
        SELECT TOP (@BatchSize) Id
        FROM dbo.Tokens WITH (READPAST, ROWLOCK)
        WHERE (TokenStatus IN (2,3))
          AND ExpiresAt < @Cutoff
        ORDER BY ExpiresAt
    )
    DELETE t
    FROM dbo.Tokens t
    INNER JOIN cte ON cte.Id = t.Id;
END
GO