using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Core.Config;

public sealed class EmailRecipientConfig : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> b)
    {
        b.ToTable("EmailRecipient", "dbo");
        b.HasKey(x => x.Id);

        b.Property(x => x.RecipientType).HasConversion<byte>();
        b.Property(x => x.Address).HasMaxLength(150).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(100);
        b.HasIndex(x => x.EmailMessageId);
    }
}