namespace Identity.Infrastructure.Configurations;

public class StateLookpConfig : IEntityTypeConfiguration<StateLookup>
{
    public void Configure(EntityTypeBuilder<StateLookup> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("StateLookups");
    }
}
