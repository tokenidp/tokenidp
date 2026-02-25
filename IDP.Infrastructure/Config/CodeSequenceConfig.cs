using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Config;

internal class CodeSequenceConfig : IEntityTypeConfiguration<CodeSequence>
{
    public void Configure(EntityTypeBuilder<CodeSequence> builder)
    {
        builder.ToTable("CodeSequences");

        builder.HasKey(x => new { x.TenantId, x.SequenceKey });

        builder.Property(x => x.SequenceKey)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(x => x.LastValue).IsRequired();
  
        builder.HasIndex(x => new { x.TenantId, x.SequenceKey }).IsUnique();
    }
}
