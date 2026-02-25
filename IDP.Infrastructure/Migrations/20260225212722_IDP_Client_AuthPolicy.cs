using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDP_Client_AuthPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientAuthPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AllowLocalLoginOverride = table.Column<bool>(type: "bit", nullable: false),
                    AllowSelfRegistrationOverride = table.Column<bool>(type: "bit", nullable: false),
                    MfaPolicyOverride = table.Column<bool>(type: "bit", nullable: false),
                    ShowExternalProviders = table.Column<bool>(type: "bit", nullable: false),
                    ShowStaySignedIn = table.Column<bool>(type: "bit", nullable: false),
                    ShowCreateAccountLink = table.Column<bool>(type: "bit", nullable: false),
                    ClientId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAuthPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientAuthPolicies_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientAuthPolicies_Clients_ClientId1",
                        column: x => x.ClientId1,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientExternalProviders",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ExternalProviderId = table.Column<int>(type: "int", nullable: false),
                    EnabledForClient = table.Column<bool>(type: "bit", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientExternalProviders", x => new { x.ClientId, x.ExternalProviderId });
                    table.ForeignKey(
                        name: "FK_ClientExternalProviders_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientExternalProviders_TenantExternalProviders_ExternalProviderId",
                        column: x => x.ExternalProviderId,
                        principalTable: "TenantExternalProviders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientAuthPolicies_ClientId",
                table: "ClientAuthPolicies",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientAuthPolicies_ClientId1",
                table: "ClientAuthPolicies",
                column: "ClientId1",
                unique: true,
                filter: "[ClientId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExternalProviders_ClientId_EnabledForClient",
                table: "ClientExternalProviders",
                columns: new[] { "ClientId", "EnabledForClient" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientExternalProviders_ClientId_ExternalProviderId",
                table: "ClientExternalProviders",
                columns: new[] { "ClientId", "ExternalProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientExternalProviders_ExternalProviderId",
                table: "ClientExternalProviders",
                column: "ExternalProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientAuthPolicies");

            migrationBuilder.DropTable(
                name: "ClientExternalProviders");
        }
    }
}
