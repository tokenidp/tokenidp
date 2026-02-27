namespace IDP.Infrastructure.Config;

internal class ClientApiResourceConfig : IEntityTypeConfiguration<ClientApiResource>
{
    public void Configure(EntityTypeBuilder<ClientApiResource> builder)
    {
        builder.ToTable("ClientApiResources");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasOne(s => s.Client)
            .WithMany(c => c.ClientApiResources)
            .HasForeignKey(x => x.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);

        // Prevent duplicate permissions per client
        builder.HasIndex(x => new { x.ClientId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_ClientApiResources_ClientId_PermissionId");

        // Fast loading of all permissions for a client
        builder.HasIndex(x => x.ClientId)
            .HasDatabaseName("IX_ClientApiResources_ClientId");
    }
}