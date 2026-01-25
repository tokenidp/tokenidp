namespace IDP.Infrastructure.Config;

internal class LookupTypeConfig : IEntityTypeConfiguration<LookupType>
{
    public void Configure(EntityTypeBuilder<LookupType> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("LookupTypes");
    }
}