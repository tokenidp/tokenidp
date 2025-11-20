namespace IDP.Core.Infrastructure.Config;

public class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Tenants");
    }
}
