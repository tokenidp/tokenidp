namespace IDP.Infrastructure.Config;

internal class LookupValueConfig : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("LookupValues");
    }
}