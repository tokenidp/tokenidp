using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboxEventId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    ActorType = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetType = table.Column<int>(type: "int", nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeSequences",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SequenceKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSequences", x => new { x.TenantId, x.SequenceKey });
                });

            migrationBuilder.CreateTable(
                name: "DashboardMetricRankings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DimensionKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BucketType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BucketStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    MetricValue = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetricRankings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DimensionKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BucketType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BucketStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BucketEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetricValue = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardMetricsCheckpoints",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetricsCheckpoints", x => new { x.TenantId, x.MetricKey });
                });

            migrationBuilder.CreateTable(
                name: "EmailDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailMessageId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<byte>(type: "tinyint", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MessageKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    PayloadMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ToAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FromAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateModelJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartitionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreAuthorizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GrantType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MfaCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is2FAVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RoleDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveUserId = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])", stored: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveUserId = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])", stored: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenReadModel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboxEventId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SourceTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenIdHash = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    TokenType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GrantType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IssuedByIp = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IssuedUserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenReadModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrantType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedByIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEventConsumers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboxEventId = table.Column<long>(type: "bigint", nullable: false),
                    ConsumerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEventConsumers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxEventConsumers_OutboxEvents_OutboxEventId",
                        column: x => x.OutboxEventId,
                        principalTable: "OutboxEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LogoutRedirectUri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClientSecretExpiry = table.Column<int>(type: "int", nullable: true),
                    AccessTokenLifetime = table.Column<int>(type: "int", nullable: false),
                    AuthorizationCodeLifetime = table.Column<int>(type: "int", nullable: false),
                    RefreshTokenExpiration = table.Column<int>(type: "int", nullable: false),
                    PermitLimit = table.Column<int>(type: "int", nullable: true),
                    TimeWindow = table.Column<TimeSpan>(type: "time", nullable: true),
                    QueueLimit = table.Column<int>(type: "int", nullable: true),
                    EnableITracking = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveUserId = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])", stored: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ControlType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Permissions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantAuthSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AllowLocalLogin = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmailVerification = table.Column<bool>(type: "bit", nullable: false),
                    AllowSelfRegistration = table.Column<bool>(type: "bit", nullable: false),
                    AuthenticationMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorCodeExpiry = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuthSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAuthSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantExternalProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ProviderType = table.Column<byte>(type: "tinyint", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Authority = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CallbackPath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantExternalProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantExternalProviders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUISettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LoginText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUISettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUISettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveUserId = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])", stored: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceTokens_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    ParentTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientAudiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAudiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientAudiences_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientGrantTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AllowedGrantType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGrantTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGrantTypes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientScopes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSecrets_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientApiResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientApiResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientApiResources_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientApiResources_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ByActor",
                table: "Activities",
                columns: new[] { "TenantId", "ActorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ByEventType",
                table: "Activities",
                columns: new[] { "TenantId", "EventType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ByStatus",
                table: "Activities",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_OccurredAtUtc",
                table: "Activities",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_Tenant_Filters",
                table: "Activities",
                columns: new[] { "TenantId", "CreatedAtUtc", "EventType", "ActorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationCodes_Code",
                table: "AuthorizationCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationCodes_Exchange",
                table: "AuthorizationCodes",
                columns: new[] { "Code", "ClientId", "IsUsed", "Expiry" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId",
                table: "ClientApiResources",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId_PermissionId",
                table: "ClientApiResources",
                columns: new[] { "ClientId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_PermissionId",
                table: "ClientApiResources",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientAudiences_ClientId_IsActive",
                table: "ClientAudiences",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientAudiences_ClientId_Name",
                table: "ClientAudiences",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientGrantTypes_ClientId_AllowedGrantType",
                table: "ClientGrantTypes",
                columns: new[] { "ClientId", "AllowedGrantType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ByTenant",
                table: "Clients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId",
                table: "Clients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_IsActive",
                table: "Clients",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Lookup",
                table: "Clients",
                columns: new[] { "TenantId", "ClientType", "TokenType", "IsActive", "ClientName" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientScopes_ClientId_Scope",
                table: "ClientScopes",
                columns: new[] { "ClientId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ClientId_SecretHash",
                table: "ClientSecrets",
                columns: new[] { "ClientId", "SecretHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ExpiresAt",
                table: "ClientSecrets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_Validation",
                table: "ClientSecrets",
                columns: new[] { "ClientId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSequences_TenantId_SequenceKey",
                table: "CodeSequences",
                columns: new[] { "TenantId", "SequenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_ByScope",
                table: "Configurations",
                columns: new[] { "TenantId", "Scope", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_EffectiveUser",
                table: "Configurations",
                column: "EffectiveUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Lookup",
                table: "Configurations",
                columns: new[] { "TenantId", "ConfigKey", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_Tenant_Scope_Key",
                table: "Configurations",
                columns: new[] { "TenantId", "Scope", "ConfigKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricRanking_ByDimension",
                table: "DashboardMetricRankings",
                columns: new[] { "TenantId", "MetricKey", "BucketType", "BucketStart", "DimensionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricRanking_TimeSeries",
                table: "DashboardMetricRankings",
                columns: new[] { "TenantId", "MetricKey", "DimensionKey", "BucketStart" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricRanking_UniqueRank",
                table: "DashboardMetricRankings",
                columns: new[] { "TenantId", "MetricKey", "BucketType", "BucketStart", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetrics_ByDimension",
                table: "DashboardMetrics",
                columns: new[] { "TenantId", "MetricKey", "DimensionKey", "BucketStart" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetrics_TimeSeries",
                table: "DashboardMetrics",
                columns: new[] { "TenantId", "MetricKey", "BucketType", "BucketStart" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetrics_UniqueBucket",
                table: "DashboardMetrics",
                columns: new[] { "TenantId", "MetricKey", "BucketType", "BucketStart", "DimensionKey" },
                unique: true,
                filter: "[DimensionKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricsCheckpoints_Key",
                table: "DashboardMetricsCheckpoints",
                columns: new[] { "TenantId", "MetricKey" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryAttempts_Message_AttemptNo",
                table: "EmailDeliveryAttempts",
                columns: new[] { "EmailMessageId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryAttempts_MessageId",
                table: "EmailDeliveryAttempts",
                column: "EmailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Dequeue",
                table: "EmailMessages",
                columns: new[] { "Status", "NextAttemptAtUtc", "LockedUntilUtc", "Priority", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Retry",
                table: "EmailMessages",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Tenant_MessageKey",
                table: "EmailMessages",
                columns: new[] { "TenantId", "MessageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Tenant_Status_Time",
                table: "EmailMessages",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEventConsumers_ByConsumer",
                table: "OutboxEventConsumers",
                columns: new[] { "ConsumerName", "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEventConsumers_Dequeue",
                table: "OutboxEventConsumers",
                columns: new[] { "Status", "NextAttemptAt", "LockedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEventConsumers_Event_Consumer",
                table: "OutboxEventConsumers",
                columns: new[] { "OutboxEventId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_AggregateReplay",
                table: "OutboxEvents",
                columns: new[] { "AggregateType", "AggregateId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_CreatedAtUtc",
                table: "OutboxEvents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_PartitionKey",
                table: "OutboxEvents",
                column: "PartitionKey");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Tenant_Time",
                table: "OutboxEvents",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "PermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Tenant_Key",
                table: "Permissions",
                columns: new[] { "TenantId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Tenant_Parent_Sequence",
                table: "Permissions",
                columns: new[] { "TenantId", "ParentId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizations_CorrelationId",
                table: "PreAuthorizations",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreAuthorizations_Lookup",
                table: "PreAuthorizations",
                columns: new[] { "CorrelationId", "UserId", "Expiry", "Is2FAVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_TokenHash",
                table: "ReferenceTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_TokenId",
                table: "ReferenceTokens",
                column: "TokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenId",
                table: "RefreshTokens",
                column: "TokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_EffectiveUserId",
                table: "Roles",
                column: "EffectiveUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Tenant_List",
                table: "Roles",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Tenant_Name",
                table: "Roles",
                columns: new[] { "TenantId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuthSettings_TenantId",
                table: "TenantAuthSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuthSettings_TenantId_AuthenticationMode",
                table: "TenantAuthSettings",
                columns: new[] { "TenantId", "AuthenticationMode" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantExternalProviders_TenantId_ProviderType",
                table: "TenantExternalProviders",
                columns: new[] { "TenantId", "ProviderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_EffectiveUserId",
                table: "Tenants",
                column: "EffectiveUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_List",
                table: "Tenants",
                columns: new[] { "IsActive", "TenantName" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                table: "Tenants",
                column: "TenantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantName",
                table: "Tenants",
                column: "TenantName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUISettings_TenantId",
                table: "TenantUISettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenReadModel_OutboxEventId",
                table: "TokenReadModel",
                column: "OutboxEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenReadModel_Tenant_Client_Status",
                table: "TokenReadModel",
                columns: new[] { "TenantId", "ClientId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenReadModel_Tenant_Status_Expiry",
                table: "TokenReadModel",
                columns: new[] { "TenantId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenReadModel_Tenant_User_Time",
                table: "TokenReadModel",
                columns: new[] { "TenantId", "UserId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ByClient_Status",
                table: "Tokens",
                columns: new[] { "TenantId", "ClientId", "TokenStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Introspection",
                table: "Tokens",
                columns: new[] { "Id", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Revoke_BySession",
                table: "Tokens",
                columns: new[] { "TenantId", "SessionId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Revoke_ByUser",
                table: "Tokens",
                columns: new[] { "TenantId", "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_User_Active",
                table: "UserAddresses",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserContacts_User_Active",
                table: "UserContacts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserContacts_User_Email",
                table: "UserContacts",
                columns: new[] { "UserId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserContacts_User_Phone",
                table: "UserContacts",
                columns: new[] { "UserId", "PhoneNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_User_Role",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByEmail",
                table: "Users",
                columns: new[] { "TenantId", "Email", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByUserName",
                table: "Users",
                columns: new[] { "TenantId", "UserName", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_Time",
                table: "Users",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_UserCode",
                table: "Users",
                columns: new[] { "TenantId", "UserCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_UserName",
                table: "Users",
                columns: new[] { "TenantId", "UserName" },
                unique: true);

            var basePath = AppContext.BaseDirectory;
            var scriptPath1 = Path.Combine(basePath, "Scripts", "vConfigurationSearch.sql");
            var scriptPath2 = Path.Combine(basePath, "Scripts", "vRoleSearch.sql");
            var scriptPath3 = Path.Combine(basePath, "Scripts", "vTenantSearch.sql");
            var scriptPath4 = Path.Combine(basePath, "Scripts", "vTokenSearch.sql");
            var scriptPath5 = Path.Combine(basePath, "Scripts", "vUserRolePermissions.sql");
            var scriptPath6 = Path.Combine(basePath, "Scripts", "vUserSearch.sql");

            var scriptPath7 = Path.Combine(basePath, "Scripts", "usp_MarkExpiredTokens.sql");
            var scriptPath8 = Path.Combine(basePath, "Scripts", "usp_PurgeOldTokens.sql");

            var viewSql1 = File.ReadAllText(scriptPath1);
            migrationBuilder.Sql(viewSql1);

            var viewSql2 = File.ReadAllText(scriptPath2);
            migrationBuilder.Sql(viewSql2);

            var viewSql3 = File.ReadAllText(scriptPath3);
            migrationBuilder.Sql(viewSql3);

            var viewSql4 = File.ReadAllText(scriptPath4);
            migrationBuilder.Sql(viewSql4);

            var viewSql5 = File.ReadAllText(scriptPath5);
            migrationBuilder.Sql(viewSql5);

            var viewSql6 = File.ReadAllText(scriptPath6);
            migrationBuilder.Sql(viewSql6);

            var viewSql7 = File.ReadAllText(scriptPath7);
            migrationBuilder.Sql(viewSql7);

            var viewSql8 = File.ReadAllText(scriptPath8);
            migrationBuilder.Sql(viewSql8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "AuthorizationCodes");

            migrationBuilder.DropTable(
                name: "ClientApiResources");

            migrationBuilder.DropTable(
                name: "ClientAudiences");

            migrationBuilder.DropTable(
                name: "ClientGrantTypes");

            migrationBuilder.DropTable(
                name: "ClientScopes");

            migrationBuilder.DropTable(
                name: "ClientSecrets");

            migrationBuilder.DropTable(
                name: "CodeSequences");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "DashboardMetricRankings");

            migrationBuilder.DropTable(
                name: "DashboardMetrics");

            migrationBuilder.DropTable(
                name: "DashboardMetricsCheckpoints");

            migrationBuilder.DropTable(
                name: "EmailDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "OutboxEventConsumers");

            migrationBuilder.DropTable(
                name: "PreAuthorizations");

            migrationBuilder.DropTable(
                name: "ReferenceTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "TenantAuthSettings");

            migrationBuilder.DropTable(
                name: "TenantExternalProviders");

            migrationBuilder.DropTable(
                name: "TenantUISettings");

            migrationBuilder.DropTable(
                name: "TokenReadModel");

            migrationBuilder.DropTable(
                name: "UserAddresses");

            migrationBuilder.DropTable(
                name: "UserContacts");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vConfigurationSearch;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vRoleSearch;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vTenantSearch;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vTokenSearch;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vUserRolePermissions;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vUserSearch;");

            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS usp_MarkExpiredTokens;");
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS usp_PurgeOldTokens;");
        }
    }
}
