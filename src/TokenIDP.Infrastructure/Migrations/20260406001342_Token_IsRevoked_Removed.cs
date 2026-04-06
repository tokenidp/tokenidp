using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Token_IsRevoked_Removed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tokens_Introspection",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Revoke_BySession",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Revoke_ByUser",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "Tokens");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Introspection",
                table: "Tokens",
                columns: new[] { "Id", "TokenStatus", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Revoke_BySession",
                table: "Tokens",
                columns: new[] { "TenantId", "SessionId", "TokenStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Revoke_ByUser",
                table: "Tokens",
                columns: new[] { "TenantId", "UserId", "TokenStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tokens_Introspection",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Revoke_BySession",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Tokens_Revoke_ByUser",
                table: "Tokens");

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "Tokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
        }
    }
}
