using TokenIDP.Domain.ReadModels;

namespace TokenIDP.Infrastructure.Config;

public sealed class DashboardMetricsCheckpointConfig
    : IEntityTypeConfiguration<DashboardMetricsCheckpoint>
{
    public void Configure(EntityTypeBuilder<DashboardMetricsCheckpoint> builder)
    {
        builder.ToTable("DashboardMetricsCheckpoints");

        builder.HasKey(x => new { x.TenantId, x.MetricKey });

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MetricKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastProcessedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Fast lookup for resume
        builder.HasIndex(x => new { x.TenantId, x.MetricKey })
            .HasDatabaseName("IX_DashboardMetricsCheckpoints_Key");
    }
}
