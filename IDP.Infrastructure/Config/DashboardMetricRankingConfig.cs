using IDP.Domain.ReadModels;

namespace IDP.Infrastructure.Config;

public sealed class DashboardMetricRankingConfig
    : IEntityTypeConfiguration<DashboardMetricRanking>
{
    public void Configure(EntityTypeBuilder<DashboardMetricRanking> builder)
    {
        builder.ToTable("DashboardMetricRankings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
               .IsRequired();

        builder.Property(x => x.BucketType)
               .HasConversion<string>()
               .HasColumnName("BucketType")
               .HasMaxLength(20)
               .IsRequired();
    }
}

