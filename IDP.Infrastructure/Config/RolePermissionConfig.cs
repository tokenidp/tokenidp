namespace IDP.Infrastructure.Config;

internal class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(p => new { p.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PermissionKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsAllowed).IsRequired();

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
