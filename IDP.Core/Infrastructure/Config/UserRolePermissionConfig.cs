namespace IDP.Core.Infrastructure.Config;

public class UserRolePermissionConfig : IEntityTypeConfiguration<UserRolePermission>
{
    public void Configure(EntityTypeBuilder<UserRolePermission> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vUserClaims");
    }
}
