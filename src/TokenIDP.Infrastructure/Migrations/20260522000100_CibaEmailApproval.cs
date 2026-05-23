using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TokenIDP.Infrastructure.Persistence;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260522000100_CibaEmailApproval")]
    public partial class CibaEmailApproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicRequestId",
                table: "BackchannelAuthenticationRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalTokenHash",
                table: "BackchannelAuthenticationRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenCreatedAtUtc",
                table: "BackchannelAuthenticationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenExpiresAtUtc",
                table: "BackchannelAuthenticationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenConsumedAtUtc",
                table: "BackchannelAuthenticationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalTokenUserHintHash",
                table: "BackchannelAuthenticationRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalLinkSentAtUtc",
                table: "BackchannelAuthenticationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDecisionAtUtc",
                table: "BackchannelAuthenticationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecisionByUserId",
                table: "BackchannelAuthenticationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionIpAddress",
                table: "BackchannelAuthenticationRequests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionUserAgent",
                table: "BackchannelAuthenticationRequests",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackchannelAuthenticationRequests_PublicRequestId",
                table: "BackchannelAuthenticationRequests",
                column: "PublicRequestId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackchannelAuthenticationRequests_PublicRequestId",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalLinkSentAtUtc",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalDecisionAtUtc",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenConsumedAtUtc",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenCreatedAtUtc",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenExpiresAtUtc",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenHash",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenUserHintHash",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "DecisionByUserId",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "DecisionIpAddress",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "DecisionUserAgent",
                table: "BackchannelAuthenticationRequests");

            migrationBuilder.DropColumn(
                name: "PublicRequestId",
                table: "BackchannelAuthenticationRequests");
        }
    }
}
