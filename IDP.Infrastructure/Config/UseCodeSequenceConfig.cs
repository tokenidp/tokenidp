namespace IDP.Infrastructure.Config;

internal class UseCodeSequenceConfig : IEntityTypeConfiguration<UserCodeSequence>
{
    public void Configure(EntityTypeBuilder<UserCodeSequence> builder)
    {
        builder.ToTable("UserCodeSequences");

        builder.HasNoKey();
    }
}
