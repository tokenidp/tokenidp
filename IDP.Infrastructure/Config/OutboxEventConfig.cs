using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Config;

internal class OutboxEventConfig : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(128);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(1024);
        builder.Property(x => x.PartitionKey).HasMaxLength(128);

        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });

        builder.Property(p => p.Status)
              .HasConversion(
                  v => v.ToString(),
                  v => Enum.Parse<OutboxStatus>(v));
    }
}