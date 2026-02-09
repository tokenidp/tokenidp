using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailMessageConfig : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> b)
    {
        b.ToTable("EmailMessage", "dbo");
        b.HasKey(x => x.Id);

        b.Property(x => x.MessageKey).HasMaxLength(120).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.MessageKey }).IsUnique();

        b.Property(x => x.Provider).HasMaxLength(30);
        b.Property(x => x.FromAddress).HasMaxLength(150);
        b.Property(x => x.FromName).HasMaxLength(100);
        b.Property(x => x.Subject).HasMaxLength(250);
        b.Property(x => x.TemplateKey).HasMaxLength(100);
        b.Property(x => x.Tags).HasMaxLength(250);

        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.PayloadMode).HasConversion<byte>();

        // collections are private fields
        b.HasMany<EmailRecipient>("_recipients")
            .WithOne()
            .HasForeignKey(r => r.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation("_recipients").UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany<EmailAttachment>("_attachments")
            .WithOne()
            .HasForeignKey(a => a.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation("_attachments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
