namespace IDP.Infrastructure.Config;

internal class TenantExternalProviderConfig : IEntityTypeConfiguration<TenantExternalProvider>
{
    public void Configure(EntityTypeBuilder<TenantExternalProvider> builder)
    {
        builder.ToTable("TenantExternalProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.ProviderType }).IsUnique();
        builder.Property(x => x.ProviderType).HasConversion<byte>().IsRequired();

        builder.Property(x => x.Enabled).IsRequired();

        // Owned Value Object: OidcClientConfig
        builder.OwnsOne(x => x.OidcConfig, cfg =>
        {
            cfg.Property(p => p.ClientId)
               .HasColumnName("ClientId")
               .HasMaxLength(250);

            cfg.Property(p => p.ClientSecret)
               .HasColumnName("ClientSecret")
               .HasMaxLength(250);

            cfg.Property(p => p.Authority)
               .HasColumnName("Authority")
               .HasMaxLength(250)
               .HasConversion(
                   v => v.ToString(),
                   v => new Uri(v));

            cfg.Property(p => p.Scopes)
               .HasColumnName("Scopes")
               .HasMaxLength(500)
               .HasConversion(
                   v => string.Join(' ', v),
                   v => v.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            cfg.Property(p => p.CallbackPath)
               .HasColumnName("CallbackPath")
               .HasMaxLength(250);
        });

        builder.HasOne(t => t.Tenant)
               .WithMany(t => t.TenantExternalProviders)
               .HasForeignKey(x => x.TenantId)
               .IsRequired();
    }
}