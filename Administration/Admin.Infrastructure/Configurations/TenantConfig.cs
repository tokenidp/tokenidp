namespace Identity.Infrastructure.Configurations;

public class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Tenants");
    }
}
