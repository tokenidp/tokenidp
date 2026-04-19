using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TokenIDP.Infrastructure.Persistence;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260418093000_TenantExternalProviderScopes")]
    public partial class TenantExternalProviderScopes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "TenantExternalProviders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "TenantExternalProviders");
        }
    }
}
