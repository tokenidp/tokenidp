using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Removed_EP_Scope_Authority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authority",
                table: "TenantExternalProviders");

            migrationBuilder.DropColumn(
                name: "CallbackPath",
                table: "TenantExternalProviders");

            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "TenantExternalProviders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authority",
                table: "TenantExternalProviders",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallbackPath",
                table: "TenantExternalProviders",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "TenantExternalProviders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
