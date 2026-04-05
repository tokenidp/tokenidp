using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_tenant_updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantDisplayName",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantKey",
                table: "Tenants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantKey",
                table: "Tenants",
                column: "TenantKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_TenantKey",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantDisplayName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantKey",
                table: "Tenants");
        }
    }
}
