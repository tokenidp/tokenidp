using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TokenIDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAdminEntitiesPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Login_ByEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login_ByUserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Tenant_Time",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_List",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Key",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Tenant_Parent_Sequence",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_ApiResources_TenantId",
                table: "ApiResources");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ApiResources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByEmail",
                table: "Users",
                columns: new[] { "TenantId", "Email", "IsDeleted", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByUserName",
                table: "Users",
                columns: new[] { "TenantId", "UserName", "IsDeleted", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_Time",
                table: "Users",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_List",
                table: "Tenants",
                columns: new[] { "IsDeleted", "IsActive", "TenantName" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                columns: new[] { "PermissionKey", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Tenant_Parent_Sequence",
                table: "Permissions",
                columns: new[] { "TenantId", "ParentId", "IsDeleted", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiResources_TenantId",
                table: "ApiResources",
                columns: new[] { "TenantId", "IsDeleted" });

            RefreshAdminSoftDeleteViews(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Login_ByEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login_ByUserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Tenant_Time",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_List",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Key",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Tenant_Parent_Sequence",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_ApiResources_TenantId",
                table: "ApiResources");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ApiResources");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByEmail",
                table: "Users",
                columns: new[] { "TenantId", "Email", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login_ByUserName",
                table: "Users",
                columns: new[] { "TenantId", "UserName", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_Time",
                table: "Users",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_List",
                table: "Tenants",
                columns: new[] { "IsActive", "TenantName" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "PermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Tenant_Parent_Sequence",
                table: "Permissions",
                columns: new[] { "TenantId", "ParentId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiResources_TenantId",
                table: "ApiResources",
                column: "TenantId");

            RestoreAdminSoftDeleteViews(migrationBuilder);
        }

        private static void RefreshAdminSoftDeleteViews(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vConfigurationSearch]
AS
      Select r.Id,
      t.TenantName,
      r.ConfigKey,
      r.ConfigValue,
      CASE WHEN u.Id is NULL THEN 'System' ELSE u.FirstName END FirstName,
      CASE WHEN u.Id is NULL THEN 'Administrator' ELSE u.LastName END LastName,
      r.IsEditable
      From dbo.[Configurations] r
      INNER JOIN dbo.Tenants t on t.Id = r.TenantId
        AND (t.IsDeleted = 0 OR t.IsDeleted IS NULL)
      LEFT JOIN dbo.Users u on u.Id = r.EffectiveUserId
        AND (u.IsDeleted = 0 OR u.IsDeleted IS NULL)
      Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vRoleSearch]
AS
      Select r.Id,
      r.TenantId,
      r.RoleName,
      CASE WHEN COALESCE(r.IsActive, 1) = 1 THEN 'Yes' ELSE 'No' END AS Active,
      CASE WHEN u.Id is NULL THEN 'System' ELSE u.FirstName END FirstName,
      CASE WHEN u.Id is NULL THEN 'Administrator' ELSE u.LastName END LastName
      From dbo.Roles r
      LEFT JOIN dbo.Users u on u.Id = r.EffectiveUserId
        AND (u.IsDeleted = 0 OR u.IsDeleted IS NULL)
      Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vTenantSearch]
AS
      Select t.Id,
      t.TenantName,
      t.TenantCode,
      t.Email,
      Case When ISNULL(t.IsActive, 1) = 1 then 'Yes' else 'No' end Active,
      u.FirstName,
      u.LastName
      From dbo.Tenants t
      LEFT JOIN dbo.Users u on u.Id = t.EffectiveUserId
        AND (u.IsDeleted = 0 OR u.IsDeleted IS NULL)
      Where (t.IsDeleted = 0 OR t.IsDeleted IS NULL)
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vUserRolePermissions]
AS
SELECT
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    CAST(MAX(CASE WHEN rc.IsAllowed = 1 THEN 1 ELSE 0 END) AS bit) AS IsAllowed,
    c.Icon,
    c.AccessUrl,
    MIN(r.RoleName) AS RoleName,
    ur.UserId,
    c.ControlType
FROM dbo.[Permissions] c
INNER JOIN dbo.RolePermissions rc ON c.Id = rc.PermissionId
INNER JOIN dbo.Roles r ON rc.RoleId = r.Id
INNER JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
Where (c.IsDeleted = 0 OR c.IsDeleted IS NULL)
  AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
  AND r.IsActive = 1
GROUP BY
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    c.Icon,
    c.AccessUrl,
    ur.UserId,
    c.ControlType
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vUserSearch]
AS
      Select u.Id,
      CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
      u.TenantId,
      u.UserName,
      u.StatusId As [Status],
      u.PhoneNumber,
      u.Email,
      CONCAT(ua.AddressLine1, ' ', ua.City, ', ', ua.[State], ' ', ua.PostalCode) AS FullAddress,
      CASE WHEN up.Id is NULL THEN 'System' ELSE up.FirstName END FirstName,
      CASE WHEN up.Id is NULL THEN 'Administrator' ELSE up.LastName END LastName,
       Roles = STUFF((
            SELECT ', ' + r.RoleName
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = u.Id
              AND (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(500)'), 1, 2, '')
      From dbo.Users u
      LEFT JOIN dbo.Users up on up.Id = u.EffectiveUserId
        AND (up.IsDeleted = 0 OR up.IsDeleted IS NULL)
      Left JOIN UserAddresses ua on u.Id = ua.UserId
      Where (u.IsDeleted = 0 OR u.IsDeleted IS NULL)
""");
        }

        private static void RestoreAdminSoftDeleteViews(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vConfigurationSearch]
AS
      Select r.Id,
      t.TenantName,
      r.ConfigKey,
      r.ConfigValue,
      FirstName,
      LastName,
      r.IsEditable
      From dbo.[Configurations] r
      INNER JOIN dbo.Tenants t on t.Id = r.TenantId
      INNER JOIN dbo.Users u on u.Id = r.EffectiveUserId
      Where (r.IsDeleted = 0 OR r.IsDeleted IS NULL)
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vRoleSearch]
AS
      Select r.Id,
      r.TenantId,
      r.RoleName,
      CASE WHEN COALESCE(r.IsActive, 1) = 1 THEN 'Yes' ELSE 'No' END AS Active,
      u.FirstName,
      u.LastName
      From dbo.Roles r
      INNER JOIN dbo.Users u on u.Id = r.EffectiveUserId
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vTenantSearch]
AS
      Select t.Id,
      t.TenantName,
      t.TenantCode,
      t.Email,
      Case When ISNULL(t.IsActive, 1) = 1 then 'Yes' else 'No' end Active,
      u.FirstName,
      u.LastName
      From dbo.Tenants t
      INNER JOIN dbo.Users u on u.Id = t.EffectiveUserId
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vUserRolePermissions]
AS
SELECT
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    CAST(MAX(CASE WHEN rc.IsAllowed = 1 THEN 1 ELSE 0 END) AS bit) AS IsAllowed,
    c.Icon,
    c.AccessUrl,
    MIN(r.RoleName) AS RoleName,
    ur.UserId,
    c.ControlType
FROM dbo.[Permissions] c
INNER JOIN dbo.RolePermissions rc ON c.Id = rc.PermissionId
INNER JOIN dbo.Roles r ON rc.RoleId = r.Id
INNER JOIN dbo.UserRoles ur ON r.Id = ur.RoleId
Where r.IsActive = 1
GROUP BY
    c.Id,
    c.[Sequence],
    c.ParentId,
    c.Permissionkey,
    c.PermissionName,
    c.Icon,
    c.AccessUrl,
    ur.UserId,
    c.ControlType
""");

            migrationBuilder.Sql(
"""
CREATE OR ALTER VIEW [dbo].[vUserSearch]
AS
      Select u.Id,
      CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
      u.TenantId,
      u.UserName,
      u.StatusId As [Status],
      u.PhoneNumber,
      u.Email,
      CONCAT(ua.AddressLine1, ' ', ua.City, ', ', ua.[State], ' ', ua.PostalCode) AS FullAddress,
      up.FirstName,
      up.LastName,
       Roles = STUFF((
            SELECT ', ' + r.RoleName
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = u.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(500)'), 1, 2, '')
      From dbo.Users u
      INNER JOIN dbo.Users up on up.Id = u.EffectiveUserId
      Left JOIN UserAddresses ua on u.Id = ua.UserId
""");
        }
    }
}
