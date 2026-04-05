using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_tenant_policy_updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RememberMe",
                table: "AuthorizationCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RememberMe",
                table: "AuthorizationCodes");
        }
    }
}
