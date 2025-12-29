namespace Admin.Core.Infrastructure.Config;

internal class ConfigurationConfig : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Configurations");
    }
}
