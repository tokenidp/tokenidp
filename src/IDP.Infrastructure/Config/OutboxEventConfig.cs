using IDP.Domain.AggregateRoots.Outbox;

namespace IDP.Infrastructure.Config;

internal class OutboxEventConfig : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AggregateId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AggregateType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PartitionKey).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CorrelationId).IsRequired();

        builder.HasMany(s => s.OutboxEventConsumers)
            .WithOne(e => e.OutboxEvent)
            .HasForeignKey(ur => ur.OutboxEventId)
            .IsRequired();

        // Tenant isolation
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxEvents_Tenant_Time");

        // Worker ordering / scanning
        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("IX_OutboxEvents_CreatedAtUtc");

        // Aggregate replay / debugging
        builder.HasIndex(x => new { x.AggregateType, x.AggregateId, x.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxEvents_AggregateReplay");

        // Partitioning / sharding (optional but useful)
        builder.HasIndex(x => x.PartitionKey)
            .HasDatabaseName("IX_OutboxEvents_PartitionKey");
    }
}