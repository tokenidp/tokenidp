namespace TokenIDP.Infrastructure.Config;

internal class UserAddressConfig : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("UserAddresses");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AddressType).HasMaxLength(30).IsRequired();
        builder.Property(p => p.AddressType)
        .HasConversion(
            v => v.ToString(),
            v => Enum.Parse<AddressTypes>(v));

        builder.Property(x => x.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine2).HasMaxLength(200);

        builder.Property(x => x.City).HasMaxLength(50).IsRequired();
        builder.Property(x => x.State).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();

        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne<User>()
            .WithMany(u => u.UserAddresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Fast lookup for profile load
        builder.HasIndex(x => new { x.UserId, x.IsActive })
            .HasDatabaseName("IX_UserAddresses_User_Active");
    }
}
