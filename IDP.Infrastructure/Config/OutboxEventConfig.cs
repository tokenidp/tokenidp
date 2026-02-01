using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Config;

internal class OutboxEventConfig : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(128);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.PartitionKey).HasMaxLength(128);

        builder.HasMany(e => e.OutboxEventConsumers)
            .WithOne(e => e.OutboxEvent)
            .HasForeignKey(ur => ur.OutboxEventId).IsRequired();
    }
}