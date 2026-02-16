namespace IDP.Infrastructure.Config;

internal class ClientAudienceConfig : IEntityTypeConfiguration<ClientAudience>
{
    public void Configure(EntityTypeBuilder<ClientAudience> builder)
    {
        builder.ToTable("ClientAudiences");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ClientId).IsRequired();

        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne<Client>()
            .WithMany(c => c.ClientAudiences)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ClientAudiences_ClientId_Name");

        // Fast lookup for token validation
        builder.HasIndex(x => new { x.ClientId, x.IsActive })
            .HasDatabaseName("IX_ClientAudiences_ClientId_IsActive");

    }
}
