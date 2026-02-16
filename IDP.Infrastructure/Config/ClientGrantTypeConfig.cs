namespace IDP.Infrastructure.Config;

internal class ClientGrantTypeConfig : IEntityTypeConfiguration<ClientGrantType>
{
    public void Configure(EntityTypeBuilder<ClientGrantType> builder)
    {
        builder.ToTable("ClientGrantTypes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AllowedGrantType).HasMaxLength(50).IsRequired();

        builder.HasOne<Client>()
            .WithMany(c => c.ClientGrantTypes)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate grant types per client
        builder.HasIndex(x => new { x.ClientId, x.AllowedGrantType })
            .IsUnique()
            .HasDatabaseName("IX_ClientGrantTypes_ClientId_AllowedGrantType");
    }
}