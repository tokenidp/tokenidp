using IDP.Domain.AggregateRoots.Users;

namespace IDP.Infrastructure.Config;

internal sealed class PasswordResetTokenConfig : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnType("varbinary(32)").IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.RequestedByType).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_PasswordResetTokens_TokenHash");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_PasswordResetTokens_ExpiresAt");

        builder.HasIndex(x => new { x.UserId, x.IsUsed })
            .HasDatabaseName("IX_PasswordResetTokens_UserId_IsUsed");
    }
}