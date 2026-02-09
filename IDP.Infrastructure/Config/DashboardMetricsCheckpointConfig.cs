using IDP.Domain.ReadModels;

namespace IDP.Infrastructure.Config;

public sealed class DashboardMetricsCheckpointConfig
    : IEntityTypeConfiguration<DashboardMetricsCheckpoint>
{
    public void Configure(EntityTypeBuilder<DashboardMetricsCheckpoint> builder)
    {
        builder.ToTable("DashboardMetricsCheckpoint");

        builder.HasKey(x => x.MetricKey);

        builder.Property(x => x.MetricKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastProcessedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}