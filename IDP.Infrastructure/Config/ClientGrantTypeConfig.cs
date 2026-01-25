namespace IDP.Infrastructure.Config;

internal class ClientGrantTypeConfig : IEntityTypeConfiguration<ClientGrantType>
{
    public void Configure(EntityTypeBuilder<ClientGrantType> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("ClientGrantTypes");

        builder.Property(p => p.AllowedGrantType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<GrantTypes>(v));
    }
}