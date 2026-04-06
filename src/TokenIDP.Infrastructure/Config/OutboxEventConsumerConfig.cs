using TokenIDP.Domain.AggregateRoots.Outbox;

namespace TokenIDP.Infrastructure.Config;

internal sealed class OutboxEventConsumerConfig
    : IEntityTypeConfiguration<OutboxEventConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxEventConsumer> builder)
    {
        builder.ToTable("OutboxEventConsumers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OutboxEventId).IsRequired();
        builder.Property(x => x.ConsumerName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.RetryCount).IsRequired();
        builder.Property(x => x.LockedBy).HasMaxLength(100);

        builder.HasOne(o => o.OutboxEvent)
            .WithMany(e => e.OutboxEventConsumers)
            .HasForeignKey(x => x.OutboxEventId)
            .IsRequired();

        // Exactly-once per consumer per event
        builder.HasIndex(x => new { x.OutboxEventId, x.ConsumerName })
            .IsUnique()
            .HasDatabaseName("IX_OutboxEventConsumers_Event_Consumer");

        // Worker dequeue hot path
        builder.HasIndex(x => new
        {
            x.Status,
            x.NextAttemptAt,
            x.LockedUntil
        })
        .HasDatabaseName("IX_OutboxEventConsumers_Dequeue");

        // Consumer-specific debugging
        builder.HasIndex(x => new { x.ConsumerName, x.Status, x.NextAttemptAt })
            .HasDatabaseName("IX_OutboxEventConsumers_ByConsumer");
    }
}


