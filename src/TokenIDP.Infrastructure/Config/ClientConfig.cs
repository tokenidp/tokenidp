namespace TokenIDP.Infrastructure.Config;

internal class ClientConfig : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IconUrl).HasMaxLength(500);

        builder.Property(x => x.ClientType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TokenType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.RedirectUri).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LogoutRedirectUri).HasMaxLength(200);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.AccessTokenLifetime).IsRequired();
        builder.Property(x => x.AuthorizationCodeLifetime).IsRequired();
        builder.Property(x => x.RefreshTokenExpiration).IsRequired();
        builder.Property(x => x.RefreshTokenDeliveryMode)
            .HasConversion<int>()
            .HasDefaultValue(RefreshTokenDeliveryMode.Response)
            .IsRequired();
        builder.Property(x => x.CibaEnabled).IsRequired();
        builder.Property(x => x.CibaDefaultExpirySeconds).IsRequired();
        builder.Property(x => x.CibaMinIntervalSeconds).IsRequired();
        builder.Property(x => x.RequireCibaUserCode).IsRequired();
        builder.Property(x => x.AllowCibaLoginHint).IsRequired();
        builder.Property(x => x.AllowCibaLoginHintToken).IsRequired();
        builder.Property(x => x.AllowCibaIdTokenHint).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Property(p => p.TokenType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TokenTypes>(v));

        builder.Property(p => p.ClientType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ClientTypes>(v));

        builder.Property(p => p.BackchannelTokenDeliveryMode)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<CibaTokenDeliveryModes>(v));

        builder.HasMany(a => a.ClientGrantTypes)
            .WithOne(e => e.Client)
            .HasForeignKey(ur => ur.ClientId)
            .IsRequired();

        builder.HasMany(a => a.ClientApiResources)
            .WithOne(e => e.Client)
            .HasForeignKey(ur => ur.ClientId)
            .IsRequired();

        builder.HasMany(a => a.ClientScopes)
            .WithOne(e => e.Client)
            .HasForeignKey(ur => ur.ClientId)
            .IsRequired();

        builder.HasMany(a => a.ClientSecrets)
            .WithOne(e => e.Client)
            .HasForeignKey(ur => ur.ClientId)
            .IsRequired();

        builder.HasMany(a => a.ClientExternalProviders)
            .WithOne(e => e.Client)
            .HasForeignKey(ur => ur.ClientId)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ClientId })
            .IsUnique()
            .HasDatabaseName("UX_Clients_Tenant_ClientId");

        // Token issuance hot-path
        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.IsActive, x.IsDeleted })
            .HasDatabaseName("IX_Clients_ClientId_IsActive_IsDeleted");

        // Admin UI list
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted })
            .HasDatabaseName("IX_Clients_ByTenant");

        // Admin UI searches
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted, x.ClientType, x.TokenType, x.IsActive, x.ClientName })
            .HasDatabaseName("IX_Clients_Lookup");
    }
}

