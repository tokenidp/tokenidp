using IDP.Domain.AggregateRoots.Configurations;

namespace IDP.Infrastructure.Config;

internal class ConfigurationConfig : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.ToTable("Configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ConfigKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConfigValue).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ValueType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(50).IsRequired();

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.IsEditable).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Property(p => p.ValueType)
       .HasConversion(
           v => v.ToString(),
           v => Enum.Parse<ValueTypes>(v));

        builder.Property(p => p.Scope)
             .HasConversion(
                 v => v.ToString(),
                 v => Enum.Parse<ConfigurationScopes>(v));

        // Computed column (SQL Server syntax)
        builder.Property(x => x.EffectiveUserId)
            .HasComputedColumnSql(
                "COALESCE(NULLIF([UpdatedBy], 0), [CreatedBy])",
                stored: true);

        // Uniqueness: one key per tenant + scope
        builder.HasIndex(x => new { x.TenantId, x.Scope, x.ConfigKey })
            .IsUnique()
            .HasDatabaseName("IX_Configurations_Tenant_Scope_Key");

        // Hot lookups
        builder.HasIndex(x => new { x.TenantId, x.ConfigKey, x.IsDeleted })
            .HasDatabaseName("IX_Configurations_Lookup");

        // Admin UI filtering
        builder.HasIndex(x => new { x.TenantId, x.Scope, x.IsDeleted })
            .HasDatabaseName("IX_Configurations_ByScope");

        // Audit queries (who changed what)
        builder.HasIndex(x => x.EffectiveUserId)
            .HasDatabaseName("IX_Configurations_EffectiveUser");
    }
}
