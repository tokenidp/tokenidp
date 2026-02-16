using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailRecipientConfig : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> builder)
    {
        builder.ToTable("EmailRecipients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.RecipientType)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(100);

        builder.HasOne<EmailMessage>()
            .WithMany(m => m.Recipients)
            .HasForeignKey(x => x.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fast load recipients for a message
        builder.HasIndex(x => x.EmailMessageId)
            .HasDatabaseName("IX_EmailRecipients_MessageId");

        // Optional: prevent duplicate same recipient per message/type
        builder.HasIndex(x => new { x.EmailMessageId, x.RecipientType, x.Address })
            .IsUnique()
            .HasDatabaseName("IX_EmailRecipients_Message_Type_Address");
    }
}