using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_client_fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientAuthPolicies_Clients_ClientId1",
                table: "ClientAuthPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ClientAuthPolicies_ClientId1",
                table: "ClientAuthPolicies");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "ClientAuthPolicies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId1",
                table: "ClientAuthPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientAuthPolicies_ClientId1",
                table: "ClientAuthPolicies",
                column: "ClientId1",
                unique: true,
                filter: "[ClientId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientAuthPolicies_Clients_ClientId1",
                table: "ClientAuthPolicies",
                column: "ClientId1",
                principalTable: "Clients",
                principalColumn: "Id");
        }
    }
}
