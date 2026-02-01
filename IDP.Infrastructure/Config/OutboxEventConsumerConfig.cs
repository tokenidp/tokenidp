using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Config;

internal sealed class OutboxEventConsumerConfig
    : IEntityTypeConfiguration<OutboxEventConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxEventConsumer> builder)
    {
        builder.ToTable("OutboxEventConsumers");

        builder.HasKey(x => x.Id);
     
        builder.HasOne(e => e.OutboxEvent)
            .WithMany(e => e.OutboxEventConsumers)
            .HasForeignKey(ur => ur.OutboxEventId)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<byte>();
    }
}

