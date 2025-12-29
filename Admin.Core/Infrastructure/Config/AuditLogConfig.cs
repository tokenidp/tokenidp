namespace Admin.Core.Infrastructure.Config;

internal class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AuditLogs");
    }
}
