namespace Identity.Infrastructure.Configurations;

public class AppConfigurationConfig : IEntityTypeConfiguration<AppConfiguration>
{
    public void Configure(EntityTypeBuilder<AppConfiguration> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("AppConfigurations");
    }
}
