namespace IDP.Infrastructure.Config;

internal class UserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("UserRoles");
    }
}