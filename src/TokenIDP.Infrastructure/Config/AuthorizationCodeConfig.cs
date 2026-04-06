using TokenIDP.Domain.AggregateRoots.Authorization;

namespace TokenIDP.Infrastructure.Config;

internal class AuthorizationCodeConfig : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("AuthorizationCodes");

        builder.HasKey(p => new { p.Id });
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();

        builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RedirectUri).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CodeChallenge).HasMaxLength(100);
        builder.Property(x => x.CodeChallengeMethod).HasMaxLength(10);
        builder.Property(x => x.Scopes).HasMaxLength(200);

        builder.Property(x => x.Expiry).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Security: prevent duplicate active codes
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("IX_AuthorizationCodes_Code");

        // Token exchange hot-path
        builder.HasIndex(x => new { x.Code, x.ClientId, x.IsUsed, x.Expiry })
            .HasDatabaseName("IX_AuthorizationCodes_Exchange");
    }
}
