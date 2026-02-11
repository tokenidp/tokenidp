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

        b.HasMany(x => x.Recipients)
        .WithOne()
        .HasForeignKey(a => a.EmailMessageId)
        .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Recipients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Attachments)
        .WithOne()
        .HasForeignKey(a => a.EmailMessageId)
        .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
