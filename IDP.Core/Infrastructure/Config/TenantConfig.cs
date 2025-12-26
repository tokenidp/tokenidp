using IDP.Core.Domain.AggregateRoots.Tenants;

namespace IDP.Core.Infrastructure.Config;

internal class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Tenants");
    }
}
