namespace IDP.Infrastructure.Config;

internal class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("RolePermissions");

        builder.HasOne(e => e.Role)
        .WithMany(e => e.RolePermissions)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();

        builder.HasOne(e => e.Permission)
       .WithMany(e => e.RolePermissions)
       .HasForeignKey(ur => ur.PermissionId)
       .IsRequired();
    }
}
