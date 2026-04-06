using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApiResources_Implemenation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientApiResources_Permissions_PermissionId",
                table: "ClientApiResources");

            migrationBuilder.DropTable(
                name: "ClientAudiences");

            migrationBuilder.DropIndex(
                name: "IX_ClientApiResources_ClientId",
                table: "ClientApiResources");

            migrationBuilder.DropIndex(
                name: "IX_ClientApiResources_ClientId_PermissionId",
                table: "ClientApiResources");

            migrationBuilder.DropIndex(
                name: "IX_ClientApiResources_PermissionId",
                table: "ClientApiResources");

            migrationBuilder.DropColumn(
                name: "PermissionId",
                table: "ClientApiResources");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ClientApiResources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ClientApiResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ApiResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiScopes_ApiResources_ApiResourceId",
                        column: x => x.ApiResourceId,
                        principalTable: "ApiResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId_IsActive",
                table: "ClientApiResources",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId_Name",
                table: "ClientApiResources",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiResources_TenantId",
                table: "ApiResources",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiResources_TenantId_Name",
                table: "ApiResources",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopes_ApiResourceId_Name",
                table: "ApiScopes",
                columns: new[] { "ApiResourceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiScopes_Name",
                table: "ApiScopes",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiScopes");

            migrationBuilder.DropTable(
                name: "ApiResources");

            migrationBuilder.DropIndex(
                name: "IX_ClientApiResources_ClientId_IsActive",
                table: "ClientApiResources");

            migrationBuilder.DropIndex(
                name: "IX_ClientApiResources_ClientId_Name",
                table: "ClientApiResources");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ClientApiResources");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ClientApiResources");

            migrationBuilder.AddColumn<int>(
                name: "PermissionId",
                table: "ClientApiResources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClientAudiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAudiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientAudiences_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId",
                table: "ClientApiResources",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_ClientId_PermissionId",
                table: "ClientApiResources",
                columns: new[] { "ClientId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientApiResources_PermissionId",
                table: "ClientApiResources",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientAudiences_ClientId_IsActive",
                table: "ClientAudiences",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientAudiences_ClientId_Name",
                table: "ClientAudiences",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientApiResources_Permissions_PermissionId",
                table: "ClientApiResources",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id");
        }
    }
}

