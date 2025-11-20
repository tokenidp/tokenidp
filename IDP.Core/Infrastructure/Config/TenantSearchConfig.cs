namespace IDP.Core.Infrastructure.Config;

public class TenantSearchConfig : IEntityTypeConfiguration<TenantSearch>
{
    public void Configure(EntityTypeBuilder<TenantSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vTenantSearch");
    }
}
