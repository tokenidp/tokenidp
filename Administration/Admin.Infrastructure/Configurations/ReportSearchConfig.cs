namespace Identity.Infrastructure.Configurations;

public class ReportSearchConfig : IEntityTypeConfiguration<ReportSearch>
{
    public void Configure(EntityTypeBuilder<ReportSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vReportSearch");
    }
}
