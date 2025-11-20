namespace IDP.Core.Infrastructure.Config;

public class RoleSearchConfig : IEntityTypeConfiguration<RoleSearch>
{
    public void Configure(EntityTypeBuilder<RoleSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vRoleSearch");
    }
}
