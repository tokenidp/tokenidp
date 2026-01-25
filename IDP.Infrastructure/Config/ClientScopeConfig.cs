namespace IDP.Infrastructure.Config;

internal class ClientScopeConfig : IEntityTypeConfiguration<ClientScope>
{
    public void Configure(EntityTypeBuilder<ClientScope> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("ClientScopes");
    }
}