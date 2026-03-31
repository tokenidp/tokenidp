namespace IDP.Infrastructure.Config;

internal class ClientApiResourceConfig : IEntityTypeConfiguration<ClientApiResource>
{
    public void Configure(EntityTypeBuilder<ClientApiResource> builder)
    {
        builder.ToTable("ClientApiResources");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ClientId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(s => s.Client)
            .WithMany(c => c.ClientApiResources)
            .HasForeignKey(x => x.ClientId)
            .IsRequired();

        builder.HasIndex(x => new { x.ClientId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ClientApiResources_ClientId_Name");

        builder.HasIndex(x => new { x.ClientId, x.IsActive })
            .HasDatabaseName("IX_ClientApiResources_ClientId_IsActive");
    }
}
