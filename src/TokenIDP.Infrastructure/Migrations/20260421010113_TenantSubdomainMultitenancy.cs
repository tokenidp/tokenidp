using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantSubdomainMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_TenantKey",
                table: "Tenants",
                newName: "UX_Tenants_TenantKey");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemTenant",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients",
                columns: new[] { "TenantId", "ClientId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UX_Clients_Tenant_ClientId",
                table: "Clients",
                columns: new[] { "TenantId", "ClientId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "UX_Clients_Tenant_ClientId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsSystemTenant",
                table: "Tenants");

            migrationBuilder.RenameIndex(
                name: "UX_Tenants_TenantKey",
                table: "Tenants",
                newName: "IX_Tenants_TenantKey");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId",
                table: "Clients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients",
                columns: new[] { "ClientId", "IsActive", "IsDeleted" });
        }
    }
}
