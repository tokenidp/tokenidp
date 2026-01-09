using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Config;

internal class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Permissions");
    }
}
