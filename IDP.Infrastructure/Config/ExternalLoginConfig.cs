namespace IDP.Infrastructure.Config;

public sealed class ExternalLoginConfiguration
    : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Provider).IsRequired().HasConversion<byte>();

        builder.Property(x => x.ProviderUserId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.DisplayName).HasMaxLength(200);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.LastLoginAtUtc);

        builder.HasIndex(x => new
        {
            x.Provider,
            x.ProviderUserId
        }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x => x.ExternalLogins)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
