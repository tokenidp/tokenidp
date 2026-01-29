namespace IDP.Infrastructure.Config;

internal class UserAddressConfig : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("UserAddresses");

        builder.Property(p => p.AddressType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<AddressTypes>(v));
    }
}