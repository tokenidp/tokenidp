namespace IDP.Infrastructure.Config;

internal class TenantPermissionConfig : IEntityTypeConfiguration<TenantPermission>
{
    public void Configure(EntityTypeBuilder<TenantPermission> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("TenantPermissions");
    }
}
