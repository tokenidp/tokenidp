using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TokenIDP.Infrastructure.Persistence;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260417170000_CibaPollSupport")]
    public partial class CibaPollSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCibaIdTokenHint",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCibaLoginHint",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCibaLoginHintToken",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BackchannelTokenDeliveryMode",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Poll");

            migrationBuilder.AddColumn<bool>(
                name: "CibaEnabled",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CibaDefaultExpirySeconds",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 300);

            migrationBuilder.AddColumn<int>(
                name: "CibaMinIntervalSeconds",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCibaUserCode",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BackchannelAuthenticationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RequestedScopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HintType = table.Column<byte>(type: "tinyint", nullable: false),
                    HintValueHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubjectHint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BindingMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserCodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuthReqIdHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    DeliveryMode = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedExpirySeconds = table.Column<int>(type: "int", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    ClientNotificationTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AcrValues = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApprovedAcr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAmr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DenialReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeniedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PollCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackchannelAuthenticationRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackchannelAuthenticationRequests_AuthReqIdHash",
                table: "BackchannelAuthenticationRequests",
                column: "AuthReqIdHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackchannelAuthenticationRequests_Client_Status_Expiry",
                table: "BackchannelAuthenticationRequests",
                columns: new[] { "TenantId", "ClientId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackchannelAuthenticationRequests_User_Status_Expiry",
                table: "BackchannelAuthenticationRequests",
                columns: new[] { "TenantId", "UserId", "Status", "ExpiresAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "AllowCibaIdTokenHint",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "AllowCibaLoginHint",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "AllowCibaLoginHintToken",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "BackchannelTokenDeliveryMode",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CibaEnabled",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CibaDefaultExpirySeconds",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CibaMinIntervalSeconds",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RequireCibaUserCode",
                table: "Clients");
        }
    }
}
