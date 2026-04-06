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

        builder.Property(x => x.ClientType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TokenType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.RedirectUri).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LogoutRedirectUri).HasMaxLength(200);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.AccessTokenLifetime).IsRequired();
        builder.Property(x => x.AuthorizationCodeLifetime).IsRequired();
        builder.Property(x => x.RefreshTokenExpiration).IsRequired();
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

        builder.HasIndex(x => x.ClientId)
            .IsUnique()
            .HasDatabaseName("IX_Clients_ClientId");

        // Token issuance hot-path
        builder.HasIndex(x => new { x.ClientId, x.IsActive })
            .HasDatabaseName("IX_Clients_ClientId_IsActive");

        // Admin UI list
        builder.HasIndex(x => new { x.TenantId })
            .HasDatabaseName("IX_Clients_ByTenant");

        // Admin UI searches
        builder.HasIndex(x => new { x.TenantId, x.ClientType, x.TokenType, x.IsActive, x.ClientName })
            .HasDatabaseName("IX_Clients_Lookup");
    }
}

