namespace IDP.Infrastructure.Config;

internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("RefreshTokens");
    }
}