using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_ExternalProvider_Role_Fixed_Reverted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoCreateUsers",
                table: "ClientExternalProviders");

            migrationBuilder.DropColumn(
                name: "DefaultRoleId",
                table: "ClientExternalProviders");

            migrationBuilder.RenameColumn(
                name: "IsEditable",
                table: "Roles",
                newName: "IsSystem");

            migrationBuilder.RenameColumn(
                name: "IsAssignableToExternalUsers",
                table: "Roles",
                newName: "IsAssignableToNewUsers");

            migrationBuilder.AddColumn<bool>(
                name: "AutoCreateUsers",
                table: "ClientAuthPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRoleId",
                table: "ClientAuthPolicies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoCreateUsers",
                table: "ClientAuthPolicies");

            migrationBuilder.DropColumn(
                name: "DefaultRoleId",
                table: "ClientAuthPolicies");

            migrationBuilder.RenameColumn(
                name: "IsSystem",
                table: "Roles",
                newName: "IsEditable");

            migrationBuilder.RenameColumn(
                name: "IsAssignableToNewUsers",
                table: "Roles",
                newName: "IsAssignableToExternalUsers");

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
    }
}
