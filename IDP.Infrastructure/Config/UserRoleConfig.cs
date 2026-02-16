namespace IDP.Infrastructure.Config;

internal class UserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(p => new { p.Id });
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.HasOne<User>()
            .WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate assignments
        builder.HasIndex(x => new { x.UserId, x.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_User_Role");

        // Authorization hot path
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        // Admin UI: users by role
        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");
    }
}