namespace IDP.Infrastructure.Config;

internal class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TenantName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TenantCode).HasMaxLength(20) .IsRequired();

        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.Theme).HasMaxLength(50);
        builder.Property(x => x.PrimaryColor).HasMaxLength(20);
        builder.Property(x => x.LogoUrl).HasMaxLength(200);
        builder.Property(x => x.HomePageUrl).HasMaxLength(200);
        builder.Property(x => x.DefaultLanguage).HasMaxLength(10);
        builder.Property(x => x.LoginText).HasMaxLength(500);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Computed column for audit
        builder.Property(x => x.EffectiveUserId)
            .HasComputedColumnSql(
                "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])",
                stored: true);

        // Unique public tenant identifier
        builder.HasIndex(x => x.TenantName)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_TenantName");

        builder.HasIndex(x => x.TenantCode)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_TenantCode");

        // Admin listing / filtering
        builder.HasIndex(x => new { x.IsActive, x.TenantName })
            .HasDatabaseName("IX_Tenants_List");

        // Audit queries
        builder.HasIndex(x => x.EffectiveUserId)
            .HasDatabaseName("IX_Tenants_EffectiveUserId");
    }
}
