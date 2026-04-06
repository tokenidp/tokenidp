using TokenIDP.Domain.ReadModels;

namespace TokenIDP.Infrastructure.Config;

public sealed class DashboardMetricRankingConfig
    : IEntityTypeConfiguration<DashboardMetricRanking>
{
    public void Configure(EntityTypeBuilder<DashboardMetricRanking> builder)
    {
        builder.ToTable("DashboardMetricRankings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.MetricKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BucketType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DimensionKey).HasMaxLength(150).IsRequired();
        builder.Property(x => x.BucketStart).IsRequired();
        builder.Property(x => x.Rank).IsRequired();
        builder.Property(x => x.MetricValue).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.Property(x => x.BucketType)
            .HasConversion<string>()
            .HasColumnName("BucketType")
            .HasMaxLength(20)
            .IsRequired();

        // Uniqueness: one row per tenant+metric+bucket+rank
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.BucketType,
            x.BucketStart,
            x.Rank
        })
        .IsUnique()
        .HasDatabaseName("IX_DashboardMetricRanking_UniqueRank");

        // Drill-down by dimension
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.BucketType,
            x.BucketStart,
            x.DimensionKey
        })
        .HasDatabaseName("IX_DashboardMetricRanking_ByDimension");

        // Time-series per dimension
        builder.HasIndex(x => new
        {
            x.TenantId,
            x.MetricKey,
            x.DimensionKey,
            x.BucketStart
        })
        .HasDatabaseName("IX_DashboardMetricRanking_TimeSeries");
    }
}


