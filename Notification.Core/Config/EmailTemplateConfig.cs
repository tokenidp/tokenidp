using IDP.Domain.AggregateRoots.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Core.Templates;

namespace Notification.Core.Config;

public sealed class EmailTemplateConfig : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TemplateKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SubjectTemplate)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        // One template per tenant + key
        builder.HasIndex(x => new { x.TenantId, x.TemplateKey })
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_Tenant_Key");

        // Hot path: resolve active template
        builder.HasIndex(x => new { x.TenantId, x.TemplateKey, x.IsActive })
            .HasDatabaseName("IX_EmailTemplates_Resolve");

        // Admin UI listing
        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.TemplateKey })
            .HasDatabaseName("IX_EmailTemplates_Tenant_List");
    }
}
