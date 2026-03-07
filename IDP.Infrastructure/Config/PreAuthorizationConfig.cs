using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Infrastructure.Config;

internal class PreAuthorizationConfig : IEntityTypeConfiguration<PreAuthorization>
{
    public void Configure(EntityTypeBuilder<PreAuthorization> builder)
    {
        builder.ToTable("PreAuthorizations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.RedirectUri).HasMaxLength(150);

        builder.Property(x => x.CodeChallenge).HasMaxLength(100);
        builder.Property(x => x.CodeChallengeMethod).HasMaxLength(10);
        builder.Property(x => x.Scopes).HasMaxLength(200);
        builder.Property(x => x.GrantType).HasMaxLength(100);

        builder.Property(x => x.MfaCode).HasMaxLength(10);
        builder.Property(x => x.Is2FAVerified);

        builder.Property(x => x.Expiry).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Uniqueness: correlation id must be unique
        builder.HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasDatabaseName("IX_PreAuthorizations_CorrelationId");

        // Hot-path: lookup by client + correlation
        builder.HasIndex(x => new { x.CorrelationId, x.UserId, x.Expiry, x.Is2FAVerified })
            .HasDatabaseName("IX_PreAuthorizations_Lookup");
    }
}