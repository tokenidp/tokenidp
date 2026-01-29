using IDP.Infrastructure.Persistence.ReadModels;

namespace IDP.Infrastructure.Config;

internal sealed class TokenReadModelConfiguration
    : IEntityTypeConfiguration<TokenReadModel>
{
    public void Configure(EntityTypeBuilder<TokenReadModel> b)
    {
        b.ToTable("TokenReadModel");

        b.HasKey(x => x.Id);

        b.Property(x => x.SourceTokenId).IsRequired();
        b.Property(x => x.SourceType).HasMaxLength(64).IsRequired();

        b.Property(x => x.TokenIdHash)
            .HasColumnType("varbinary(32)");

        b.Property(x => x.TokenType).HasMaxLength(64).IsRequired();
        b.Property(x => x.ClientId).HasMaxLength(200).IsRequired();

        b.Property(x => x.Subject).HasMaxLength(256);

        b.Property(x => x.Status).HasMaxLength(20).IsRequired();

        b.Property(x => x.Scopes).HasMaxLength(1024);
        b.Property(x => x.Audience).HasMaxLength(1024).IsRequired();

        b.Property(x => x.IssuedByIp).HasMaxLength(128);
        b.Property(x => x.IssuedUserAgent).HasMaxLength(512);
        b.Property(x => x.IssuedBy).HasMaxLength(256);

        b.Property(x => x.RevokedBy).HasMaxLength(256);
        b.Property(x => x.RevokedByIp).HasMaxLength(128);
        b.Property(x => x.RevokedReason).HasMaxLength(512);

        b.Property(x => x.CreatedOn)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasIndex(x => new { x.TenantId, x.IssuedAt });
        b.HasIndex(x => new { x.TenantId, x.UserId });
        b.HasIndex(x => new { x.TenantId, x.ClientId });
        b.HasIndex(x => new { x.SourceTokenId, x.SourceType })
            .IsUnique();
    }
}
