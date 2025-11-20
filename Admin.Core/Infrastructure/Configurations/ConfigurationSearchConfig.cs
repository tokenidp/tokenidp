namespace Identity.Infrastructure.Configurations;

public class ConfigurationSearchConfig : IEntityTypeConfiguration<ConfigurationSearch>
{
    public void Configure(EntityTypeBuilder<ConfigurationSearch> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToView("vConfigurationSearchs");
    }
}
