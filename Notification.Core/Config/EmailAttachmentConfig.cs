using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailAttachmentConfig : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> builder)
    {
        builder.ToTable("EmailAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.StorageMode)
            .IsRequired();

        builder.Property(x => x.BlobPath)
            .HasMaxLength(500);

        builder.HasOne<EmailMessage>()
            .WithMany(m => m.Attachments)
            .HasForeignKey(x => x.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fast load of attachments for an email
        builder.HasIndex(x => x.EmailMessageId)
            .HasDatabaseName("IX_EmailAttachments_EmailMessageId");

        // Optional: prevent duplicate filenames per email
        builder.HasIndex(x => new { x.EmailMessageId, x.FileName })
            .IsUnique()
            .HasDatabaseName("IX_EmailAttachments_EmailMessageId_FileName");
    }
}
