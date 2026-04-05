namespace IDP.Infrastructure.Config;

internal class ClientGrantTypeConfig : IEntityTypeConfiguration<ClientGrantType>
{
    public void Configure(EntityTypeBuilder<ClientGrantType> builder)
    {
        builder.ToTable("ClientGrantTypes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AllowedGrantType).HasMaxLength(32).IsRequired();

        builder.Property(p => p.AllowedGrantType)
            .HasConversion(
                v => v.ToString(),
                v => (GrantTypes)Enum.Parse(typeof(GrantTypes), v));

        builder.HasOne(s => s.Client)
            .WithMany(c => c.ClientGrantTypes)
            .HasForeignKey(x => x.ClientId)
            .IsRequired();

        // Prevent duplicate grant types per client
        builder.HasIndex(x => new { x.ClientId, x.AllowedGrantType })
            .IsUnique()
            .HasDatabaseName("IX_ClientGrantTypes_ClientId_AllowedGrantType");
    }
}