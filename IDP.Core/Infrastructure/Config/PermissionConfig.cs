using IDP.Core.Domain.AggregateRoots;

namespace IDP.Core.Infrastructure.Config;

internal class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Permissions");
    }
}
