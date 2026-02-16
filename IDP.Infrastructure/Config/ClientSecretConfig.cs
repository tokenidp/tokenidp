namespace IDP.Infrastructure.Config;

public sealed class ClientSecretConfig : IEntityTypeConfiguration<ClientSecret>
{
    public void Configure(EntityTypeBuilder<ClientSecret> builder)
    {
        builder.ToTable("ClientSecrets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SecretHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(100);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsRevoked).IsRequired();

        builder.HasOne<Client>()
            .WithMany(c => c.ClientSecrets)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hot path: validate active secret for client
        builder.HasIndex(x => new { x.ClientId, x.IsRevoked, x.ExpiresAt })
            .HasDatabaseName("IX_ClientSecrets_Validation");

        // Cleanup jobs (expired secrets)
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_ClientSecrets_ExpiresAt");

        builder.HasIndex(x => new { x.ClientId, x.SecretHash })
            .IsUnique()
            .HasDatabaseName("IX_ClientSecrets_ClientId_SecretHash");
    }
}

