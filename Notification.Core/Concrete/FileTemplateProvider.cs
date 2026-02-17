using IDP.Domain.AggregateRoots.Emails;
using Notification.Core.Templates;

namespace Notification.Core.Concrete;
internal sealed class FileTemplateProvider
{
    private readonly string _basePath;

    public FileTemplateProvider(string basePath)
    {
        _basePath = basePath;
    }

    public async Task<EmailTemplate?> LoadAsync(string templateKey, CancellationToken ct)
    {
        var htmlPath = Path.Combine(_basePath, $"{templateKey}.html");
        var subjectPath = Path.Combine(_basePath, $"{templateKey}.subject.txt");

        if (!File.Exists(htmlPath) && !File.Exists(subjectPath))
            return null;

        var html = File.Exists(htmlPath) ? await File.ReadAllTextAsync(htmlPath, ct) : null;
        var subject = File.Exists(subjectPath)
            ? await File.ReadAllTextAsync(subjectPath, ct)
            : templateKey;

        return new EmailTemplate(
            tenantId: 0,
            templateKey: templateKey,
            subjectTemplate: subject,
            htmlTemplate: html,
            textTemplate: null,
            isActive: true);
    }
}
