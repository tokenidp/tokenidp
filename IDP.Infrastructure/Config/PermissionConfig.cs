using IDP.Domain.AggregateRoots.Permissions;

namespace IDP.Infrastructure.Config;

internal class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.PermissionKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PermissionName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AccessUrl).HasMaxLength(200);
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.Property(x => x.ControlType).HasMaxLength(20)
             .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ControlTypes>(v));

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsSystem).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Uniqueness: permission keys per tenant (or global if IsSystem)
        builder.HasIndex(x => new { x.TenantId, x.PermissionKey })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Tenant_Key");

        // Authorization lookup
        builder.HasIndex(x => x.PermissionKey)
            .HasDatabaseName("IX_Permissions_Key");

        // Tree rendering
        builder.HasIndex(x => new { x.TenantId, x.ParentId, x.Sequence })
            .HasDatabaseName("IX_Permissions_Tenant_Parent_Sequence");

        // Self-referencing hierarchy
        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
