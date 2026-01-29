namespace IDP.Infrastructure.Config;

internal class TokenConfig : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Tokens");

        builder.Property(p => p.TokenType)
            .HasConversion(
            v => v.ToString(),
            v => (TokenTypes)Enum.Parse(typeof(TokenTypes), v));

        builder.Property(p => p.GrantType)
           .HasConversion(
           v => v.ToString(),
           v => (GrantTypes)Enum.Parse(typeof(GrantTypes), v));

        builder.Property(p => p.TokenStatus)
           .HasConversion(
           v => v.ToString(),
           v => (TokenStatus)Enum.Parse(typeof(TokenStatus), v));
    }
}