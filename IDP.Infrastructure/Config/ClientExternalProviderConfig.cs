namespace IDP.Infrastructure.Config;

internal class ClientExternalProviderConfig : IEntityTypeConfiguration<ClientExternalProvider>
{
    public void Configure(EntityTypeBuilder<ClientExternalProvider> b)
    {
        b.ToTable("ClientExternalProviders");

        b.HasKey(x => new { x.ClientId, x.ExternalProviderId });

        b.Property(x => x.ClientId).IsRequired();
        b.Property(x => x.ExternalProviderId).IsRequired();

        b.Property(x => x.EnabledForClient).IsRequired();

        b.HasIndex(x => new { x.ClientId, x.EnabledForClient });
        b.HasIndex(x => new { x.ClientId, x.ExternalProviderId });

        b.HasOne(x => x.Client)
         .WithMany(x => x.ClientExternalProviders)
         .HasForeignKey(x => x.ClientId)
         .IsRequired();

        b.HasOne<TenantExternalProvider>()
         .WithMany()
         .HasForeignKey(x => x.ExternalProviderId)
         .OnDelete(DeleteBehavior.NoAction)
         .IsRequired();
    }
}
