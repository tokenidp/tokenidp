using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Infrastructure.Config;

internal class TokenSearchConfig : IEntityTypeConfiguration<TokenSearch>
{
    public void Configure(EntityTypeBuilder<TokenSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vTokenSearch");

        builder.Property(p => p.Status)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<TokenStatus>(v));
    }
}