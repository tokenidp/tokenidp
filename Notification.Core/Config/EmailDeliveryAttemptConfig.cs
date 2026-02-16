using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailDeliveryAttemptConfig : IEntityTypeConfiguration<EmailDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryAttempt> builder)
    {
        builder.ToTable("EmailDeliveryAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.AttemptNo)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.Outcome)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(30);

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(200);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.HasOne<EmailMessage>()
            .WithMany()
            .HasForeignKey(x => x.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate attempt numbers per message
        builder.HasIndex(x => new { x.EmailMessageId, x.AttemptNo })
            .IsUnique()
            .HasDatabaseName("IX_EmailDeliveryAttempts_Message_AttemptNo");

        // Fast load of attempts per message
        builder.HasIndex(x => x.EmailMessageId)
            .HasDatabaseName("IX_EmailDeliveryAttempts_MessageId");
    }
}

