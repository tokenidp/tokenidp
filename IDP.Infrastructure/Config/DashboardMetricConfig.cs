using IDP.Domain.ReadModels;

namespace IDP.Infrastructure.Config;

public sealed class DashboardMetricConfig
    : IEntityTypeConfiguration<DashboardMetric>
{
    public void Configure(EntityTypeBuilder<DashboardMetric> builder)
    {

        builder.ToTable("DashboardMetrics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.MetricKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BucketType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DimensionKey).HasMaxLength(150);
        builder.Property(x => x.BucketStart).IsRequired();
        builder.Property(x => x.BucketEnd).IsRequired();
        builder.Property(x => x.MetricValue).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.BucketType)
            .HasConversion<string>()
            .HasColumnName("BucketType")
            .HasMaxLength(20)
            .IsRequired();

        // Uniqueness: one metric per bucket (+ dimension)
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.BucketType,
            x.BucketStart,
            x.DimensionKey
        })
        .IsUnique()
        .HasDatabaseName("IX_DashboardMetrics_UniqueBucket");

        // Time-series dashboard queries
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.BucketType,
            x.BucketStart
        })
        .HasDatabaseName("IX_DashboardMetrics_TimeSeries");

        // Dimension drill-down
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.DimensionKey,
            x.BucketStart
        })
        .HasDatabaseName("IX_DashboardMetrics_ByDimension");
    }
}
