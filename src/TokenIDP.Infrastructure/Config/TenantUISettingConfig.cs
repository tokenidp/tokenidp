namespace TokenIDP.Infrastructure.Config;

internal class TenantUISettingConfig : IEntityTypeConfiguration<TenantUISetting>
{
    public void Configure(EntityTypeBuilder<TenantUISetting> builder)
    {
        builder.ToTable("TenantUISettings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.HasIndex(x => x.TenantId).IsUnique();

        builder.Property(x => x.Theme).HasMaxLength(50);
        builder.Property(x => x.PrimaryColor).HasMaxLength(20);
        builder.Property(x => x.LogoUrl).HasMaxLength(200);
        builder.Property(x => x.DefaultLanguage).HasMaxLength(10);
        builder.Property(x => x.LoginText).HasMaxLength(500);

        builder.HasOne(x => x.Tenant)
           .WithOne(t => t.TenantUISetting)
           .HasForeignKey<TenantUISetting>(x => x.TenantId)
           .IsRequired();
    }
}

