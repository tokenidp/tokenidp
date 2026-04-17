using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAdminEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_ByTenant",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientId_IsActive",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Lookup",
                table: "Clients");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ByTenant",
                table: "Clients",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients",
                columns: new[] { "ClientId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Lookup",
                table: "Clients",
                columns: new[] { "TenantId", "IsDeleted", "ClientType", "TokenType", "IsActive", "ClientName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_ByTenant",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientId_IsActive_IsDeleted",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Lookup",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Clients");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ByTenant",
                table: "Clients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientId_IsActive",
                table: "Clients",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Lookup",
                table: "Clients",
                columns: new[] { "TenantId", "ClientType", "TokenType", "IsActive", "ClientName" });
        }
    }
}
