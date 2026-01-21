using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Config;

internal class ConfigurationConfig : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Configurations");

        builder.Property(p => p.ValueType)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ValueTypes>(v));

        builder.Property(p => p.Scope)
             .HasConversion(
                 v => v.ToString(),
                 v => Enum.Parse<ConfigurationScopes>(v));
    }
}
