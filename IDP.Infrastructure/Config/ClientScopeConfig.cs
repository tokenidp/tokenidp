namespace IDP.Infrastructure.Config;

internal class ClientScopeConfig : IEntityTypeConfiguration<ClientScope>
{
    public void Configure(EntityTypeBuilder<ClientScope> builder)
    {
        builder.ToTable("ClientScopes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Scope).HasMaxLength(50).IsRequired();

        builder.HasOne<Client>()
            .WithMany(c => c.ClientScopes)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate grant types per client
        builder.HasIndex(x => new { x.ClientId, x.Scope })
            .IsUnique()
            .HasDatabaseName("IX_ClientScopes_ClientId_Scope");
    }
}