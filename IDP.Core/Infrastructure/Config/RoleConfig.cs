namespace IDP.Core.Infrastructure.Config;

internal class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Roles");

        builder.Property(u => u.ConcurrencyStamp)
        .IsConcurrencyToken();

        builder.Property(e => e.Name).HasColumnName("RoleName");

        builder.HasMany(e => e.UserRoles)
        .WithOne(e => e.Role)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();

        builder.HasMany(e => e.RolePermissions)
        .WithOne(e => e.Role)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();
    }
}
