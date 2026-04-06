namespace TokenIDP.Infrastructure.Config;

internal sealed class EmailConfirmationTokenConfig : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.ToTable("EmailConfirmationTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash)
            .HasDatabaseName("IX_EmailConfirmationTokens_TokenHash");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_EmailConfirmationTokens_ExpiresAt");

        builder.HasIndex(x => new { x.UserId, x.IsUsed })
            .HasDatabaseName("IX_EmailConfirmationTokens_UserId_IsUsed");
    }
}
