using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailAttachmentConfig : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> b)
    {
        b.ToTable("EmailAttachment", "dbo");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.BlobPath).HasMaxLength(500);
        b.HasIndex(x => x.EmailMessageId);
    }
}
