namespace IDP.Infrastructure.Config;

internal class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Name).HasColumnName("RoleName").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(100);
        builder.Property(x => x.RoleDescription).HasMaxLength(250);
        builder.Property(x => x.IsAssignableToExternalUsers).IsRequired();

        builder.Property(x => x.ConcurrencyStamp).HasMaxLength(100).IsRequired(false).IsConcurrencyToken();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsSystem).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Computed column (same pattern you used elsewhere)
        builder.Property(x => x.EffectiveUserId)
            .HasComputedColumnSql(
                "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])",
                stored: true);

        // Uniqueness: role name per tenant
        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_Roles_Tenant_Name");

        // Tenant role listing (soft-delete aware)
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted })
            .HasDatabaseName("IX_Roles_Tenant_List");

        // ⚡ Audit queries
        builder.HasIndex(x => x.EffectiveUserId)
            .HasDatabaseName("IX_Roles_EffectiveUserId");

        builder.HasMany(e => e.RolePermissions)
        .WithOne(e => e.Role)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();
    }
}
