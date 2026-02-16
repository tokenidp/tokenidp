using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Infrastructure.Config;

internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TokenId).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnType("varbinary(32)").IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        //  Hot path: lookup by hash (introspection/refresh)
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        // Map to token metadata record (if TokenId represents it)
        builder.HasIndex(x => x.TokenId)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenId");

        // Cleanup / expiry scans
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
    }
}