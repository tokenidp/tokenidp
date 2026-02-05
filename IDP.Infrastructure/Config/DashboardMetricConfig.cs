using IDP.Domain.ReadModels;
using Microsoft.Identity.Client;

namespace IDP.Infrastructure.Config;

public sealed class DashboardMetricConfig
    : IEntityTypeConfiguration<DashboardMetric>
{
    public void Configure(EntityTypeBuilder<DashboardMetric> builder)
    {
        builder.ToTable("DashboardMetrics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.BucketType)
               .HasConversion<string>()
               .HasColumnName("BucketType")
               .HasMaxLength(20)
               .IsRequired();
    }
}
