namespace Identity.Infrastructure.Configurations;

public class AppClaimTenantConfig : IEntityTypeConfiguration<AppClaimTenant>
{
    public void Configure(EntityTypeBuilder<AppClaimTenant> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AppClaimTenants");
    }
}
