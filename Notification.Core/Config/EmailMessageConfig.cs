using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailMessageConfig : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.MessageKey)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(30);

        builder.Property(x => x.FromAddress)
            .HasMaxLength(150);

        builder.Property(x => x.FromName)
            .HasMaxLength(100);

        builder.Property(x => x.Subject)
            .HasMaxLength(250);

        builder.Property(x => x.TemplateKey)
            .HasMaxLength(100);

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(x => x.Tags)
            .HasMaxLength(250);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.Property(x => x.CancelReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
           .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.MaxAttempts)
            .IsRequired();

        builder.Property(x => x.Status).HasConversion<byte>();
        builder.Property(x => x.PayloadMode).HasConversion<byte>();

        builder.HasMany(x => x.Recipients)
        .WithOne()
        .HasForeignKey(a => a.EmailMessageId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Recipients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Attachments)
        .WithOne()
        .HasForeignKey(a => a.EmailMessageId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optional: idempotency / deduplication
        builder.HasIndex(x => new { x.TenantId, x.MessageKey })
            .IsUnique()
            .HasDatabaseName("IX_EmailMessages_Tenant_MessageKey");

        // Worker dequeue hot path
        builder.HasIndex(x => new
        {
            x.Status,
            x.NextAttemptAtUtc,
            x.LockedUntilUtc,
            x.Priority,
            x.CreatedAtUtc
        })
        .HasDatabaseName("IX_EmailMessages_Dequeue");

        // Tenant admin filtering
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc })
            .HasDatabaseName("IX_EmailMessages_Tenant_Status_Time");

        // Retry scheduling
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc })
            .HasDatabaseName("IX_EmailMessages_Retry");
    }
}
