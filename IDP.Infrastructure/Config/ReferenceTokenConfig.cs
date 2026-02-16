using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Infrastructure.Config;

internal class ReferenceTokenConfig : IEntityTypeConfiguration<ReferenceToken>
{
    public void Configure(EntityTypeBuilder<ReferenceToken> builder)
    {
        builder.ToTable("ReferenceTokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TokenId).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnType("varbinary(32)").IsRequired();

        // Hot path: introspection by hash
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_ReferenceTokens_TokenHash");

        // Admin/revocation lookup
        builder.HasIndex(x => x.TokenId)
            .IsUnique()
            .HasDatabaseName("IX_ReferenceTokens_TokenId");
    }
}