using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Config;

internal sealed class OutboxEventConsumerConfig
    : IEntityTypeConfiguration<OutboxEventConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxEventConsumer> builder)
    {
        builder.ToTable("OutboxEventConsumers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OutboxEventId)
            .IsRequired();

        builder.Property(x => x.ConsumerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);
        builder.Property(x => x.FailedAt);
        builder.Property(x => x.NextAttemptAt);

        builder.Property(x => x.LockedUntil);

        builder.Property(x => x.LockedBy)
            .HasMaxLength(100);

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.HasIndex(x => new
        {
            x.ConsumerName,
            x.Status,
            x.NextAttemptAt,
            x.LockedUntil
        })
        .HasDatabaseName("IX_OutboxConsumers_Worker");

        builder.HasIndex(x => x.OutboxEventId)
            .HasDatabaseName("IX_OutboxConsumers_Event");

        builder.HasIndex(x => new { x.OutboxEventId, x.ConsumerName })
            .IsUnique()
            .HasDatabaseName("UX_OutboxConsumers_Event_Consumer");

        builder.HasOne<OutboxEvent>()
            .WithMany()
            .HasForeignKey(x => x.OutboxEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Status)
            .HasConversion(
          v => v.ToString(),
          v => Enum.Parse<OutboxStatus>(v));
    }
}

