namespace IDP.Core.Infrastructure.Config;

public class ConfigurationConfig : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Configurations");
    }
}
