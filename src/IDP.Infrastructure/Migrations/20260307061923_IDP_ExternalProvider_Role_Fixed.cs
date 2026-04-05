using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_ExternalProvider_Role_Fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAssignableToExternalUsers",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoCreateUsers",
                table: "ClientExternalProviders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRoleId",
                table: "ClientExternalProviders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAssignableToExternalUsers",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "AutoCreateUsers",
                table: "ClientExternalProviders");

            migrationBuilder.DropColumn(
                name: "DefaultRoleId",
                table: "ClientExternalProviders");
        }
    }
}
