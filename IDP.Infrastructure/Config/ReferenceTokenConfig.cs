using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Infrastructure.Config;

internal class ReferenceTokenConfig : IEntityTypeConfiguration<ReferenceToken>
{
    public void Configure(EntityTypeBuilder<ReferenceToken> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("ReferenceTokens");
    }
}