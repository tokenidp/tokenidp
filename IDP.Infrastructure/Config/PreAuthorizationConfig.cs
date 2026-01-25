using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Infrastructure.Config;

internal class PreAuthorizationConfig : IEntityTypeConfiguration<PreAuthorization>
{
    public void Configure(EntityTypeBuilder<PreAuthorization> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("PreAuthorizations");
    }
}