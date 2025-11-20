namespace Identity.Infrastructure.Configurations;

public class AppRoleConfig : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AppRoles");

        builder.Property(u => u.ConcurrencyStamp)
        .IsConcurrencyToken();

        builder.Property(e => e.Name).HasColumnName("RoleName");

        builder.HasMany(e => e.AppUserRoles)
        .WithOne(e => e.AppRole)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();

        builder.HasMany(e => e.AppRoleClaims)
        .WithOne(e => e.AppRole)
        .HasForeignKey(ur => ur.RoleId)
        .IsRequired();
    }
}
