using IDP.Domain.ReadModels;

namespace IDP.Infrastructure.Config;

internal sealed class TokenReadModelConfiguration
    : IEntityTypeConfiguration<TokenReadModel>
{
    public void Configure(EntityTypeBuilder<TokenReadModel> builder)
    {     
        builder.ToTable("TokenReadModel");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SourceTokenId).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();

        builder.Property(x => x.TokenIdHash).HasColumnType("varbinary(32)");
        builder.Property(x => x.TokenType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GrantType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ClientId).IsRequired();

        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(256);
        builder.Property(x => x.Scopes).HasMaxLength(1024);
        builder.Property(x => x.Audience).HasMaxLength(1024).IsRequired();

        builder.Property(x => x.IssuedByIp).HasMaxLength(32);
        builder.Property(x => x.IssuedUserAgent).HasMaxLength(512);
        builder.Property(x => x.IssuedBy).HasMaxLength(256);

        builder.Property(x => x.RevokedBy).HasMaxLength(256);
        builder.Property(x => x.RevokedByIp).HasMaxLength(128);
        builder.Property(x => x.RevokedReason).HasMaxLength(512);

        builder.Property(x => x.CreatedOn).IsRequired();

        // Idempotency: one projection per outbox event
        builder.HasIndex(x => x.OutboxEventId)
            .IsUnique()
            .HasDatabaseName("IX_TokenReadModel_OutboxEventId");

        // Admin: list tokens by tenant + user
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.IssuedAt })
            .HasDatabaseName("IX_TokenReadModel_Tenant_User_Time");

        // Admin: tokens by client + status
        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_TokenReadModel_Tenant_Client_Status");

        // Active tokens monitoring
        builder.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_TokenReadModel_Tenant_Status_Expiry");
    }
}
