namespace TokenIDP.Infrastructure.Config;

internal class TenantAuthSettingConfig : IEntityTypeConfiguration<TenantAuthSetting>
{
    public void Configure(EntityTypeBuilder<TenantAuthSetting> builder)
    {
        builder.ToTable("TenantAuthSettings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.HasIndex(x => x.TenantId).IsUnique();

        builder.Property(x => x.AllowLocalLogin).IsRequired();
        builder.Property(x => x.RequireEmailVerification).IsRequired();
        builder.Property(x => x.AllowSelfRegistration).IsRequired();

        builder.Property(x => x.AuthenticationMode)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.OwnsOne(x => x.TwoFactor, tf =>
        {
            tf.Property(p => p.IsEnabled)
              .HasColumnName("TwoFactorEnabled")
              .IsRequired();

            tf.Property(p => p.CodeExpiry)
              .HasColumnName("TwoFactorCodeExpiry")
              .HasConversion(
                  v => v.HasValue ? (int?)v.Value.TotalSeconds : null,
                  v => v.HasValue ? TimeSpan.FromSeconds(v.Value) : null
              )
              .IsRequired(false);
        });

        // Optional: Shadow navigation back to Tenant (if you have Tenant aggregate)
        builder.HasOne(x => x.Tenant)
            .WithOne(t => t.TenantAuthSetting)
            .HasForeignKey<TenantAuthSetting>(x => x.TenantId)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.AuthenticationMode });
    }
}

