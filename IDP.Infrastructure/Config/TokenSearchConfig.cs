namespace IDP.Infrastructure.Config;

internal class TokenSearchConfig : IEntityTypeConfiguration<TokenSearch>
{
    public void Configure(EntityTypeBuilder<TokenSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vTokenSearch");
    }
}