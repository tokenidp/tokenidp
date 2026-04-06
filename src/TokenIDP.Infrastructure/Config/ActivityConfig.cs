using TokenIDP.Domain.ReadModels;
using TokenIDP.Domain.ReadModels.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TokenIDP.Infrastructure.Config;

internal sealed class ActivityConfig : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OutboxEventId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.Property(x => x.Category).IsRequired().HasConversion<int>();
        builder.Property(x => x.EventType).IsRequired().HasConversion<int>();
        builder.Property(x => x.Severity).IsRequired().HasConversion<int>();
        builder.Property(x => x.ActorType).IsRequired().HasConversion<int>();

        builder.Property(x => x.ActorId).HasMaxLength(50);
        builder.Property(x => x.ActorDisplayName).HasMaxLength(256);
        builder.Property(x => x.TargetType).HasConversion<int>();
        builder.Property(x => x.TargetId).HasMaxLength(64);
        builder.Property(x => x.TargetDescription).HasMaxLength(256);

        builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1024);

        builder.Property(x => x.CorrelationId);
        builder.Property(x => x.IpAddress).HasMaxLength(32);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.Property(x => x.Category)
           .HasConversion(new EnumToNumberConverter<ActivityCategory, int>())
           .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion(new EnumToNumberConverter<ActivityEventType, int>())
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion(new EnumToNumberConverter<ActivitySeverity, int>())
            .IsRequired();

        builder.Property(x => x.ActorType)
            .HasConversion(new EnumToNumberConverter<ActivityActorType, int>())
            .IsRequired();

        var targetTypeConverter = new ValueConverter<ActivityTargetType?, int?>(
            v => v.HasValue ? (int?)v.Value : null,
            v => v.HasValue ? (ActivityTargetType?)v.Value : null);

        builder.Property(x => x.TargetType)
            .HasConversion(targetTypeConverter);

        // Indexes for Activity Screen filters
        builder.HasIndex(x => x.CreatedAtUtc)
               .HasDatabaseName("IX_Activities_OccurredAtUtc");

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.CreatedAtUtc,
            x.EventType,
            x.ActorId,
            x.Status
        })
        .HasDatabaseName("IX_Activities_Tenant_Filters");

        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc })
              .HasDatabaseName("IX_Activities_ByStatus");

        builder.HasIndex(x => new { x.TenantId, x.ActorId, x.CreatedAtUtc })
               .HasDatabaseName("IX_Activities_ByActor");

        builder.HasIndex(x => new { x.TenantId, x.EventType, x.CreatedAtUtc })
               .HasDatabaseName("IX_Activities_ByEventType");
    }
}
