using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Infrastructure.Config;

internal class TokenConfig : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("Tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();

        builder.Property(x => x.TokenStatus).HasMaxLength(20).IsRequired()
             .HasConversion(
                v => v.ToString(),
                v => (TokenStatus)Enum.Parse(typeof(TokenStatus), v));

        builder.Property(x => x.TokenType).HasMaxLength(20).IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (TokenTypes)Enum.Parse(typeof(TokenTypes), v));

        builder.Property(x => x.GrantType).HasMaxLength(20).IsRequired()
             .HasConversion(
                v => v.ToString(),
                v => (GrantTypes)Enum.Parse(typeof(GrantTypes), v));

        builder.Property(x => x.Scope).HasMaxLength(200);

        builder.Property(x => x.DeviceId).HasMaxLength(100);
        builder.Property(x => x.CreatedByIpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Audience).HasMaxLength(150);
        builder.Property(x => x.RevokeReason).HasMaxLength(250);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.IsRevoked).IsRequired();

        // Hot path: token validity checks
        builder.HasIndex(x => new { x.Id, x.IsRevoked, x.ExpiresAt })
            .HasDatabaseName("IX_Tokens_Introspection");

        //  Revoke by user/session
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.IsRevoked })
            .HasDatabaseName("IX_Tokens_Revoke_ByUser");

        builder.HasIndex(x => new { x.TenantId, x.SessionId, x.IsRevoked })
            .HasDatabaseName("IX_Tokens_Revoke_BySession");

        // Client monitoring
        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.TokenStatus })
            .HasDatabaseName("IX_Tokens_ByClient_Status");
    }
}