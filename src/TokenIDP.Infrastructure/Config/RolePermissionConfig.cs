namespace TokenIDP.Infrastructure.Config;

internal class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(p => new { p.Id });
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PermissionKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsAllowed).IsRequired();

        builder.HasOne(e => e.Role)
        .WithMany(e => e.RolePermissions)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Permission)
       .WithMany()
       .HasForeignKey(ur => ur.PermissionId)
       .IsRequired()
       .OnDelete(DeleteBehavior.NoAction);
    }
}

