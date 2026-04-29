using System.Text.Json;
using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Primitives;

namespace TokenIDP.Infrastructure.Emails.Concrete;

internal sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    public Task<(string Subject, string? Html, string? Text)> RenderAsync(
        int tenantId,
        string templateKey,
        string? modelJson,
        CancellationToken ct)
    {
        var tokens = string.IsNullOrEmpty(modelJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(modelJson)!;

        if (templateKey == "MFA_CODE")
        {
            var html = ReplaceTokens(EmailTemplates.MfaCodeHtml, tokens)
                .Replace("{YEAR}", DateTime.UtcNow.Year.ToString());

            (string Subject, string? Html, string? Text) result = (EmailTemplates.MfaCodeSubject, html, default);

            return Task.FromResult(result);
        }

        if (templateKey == "PASSWORD_RESET")
        {
            var html = ReplaceTokens(EmailTemplates.PasswordResetHtml, tokens)
                .Replace("{YEAR}", DateTime.UtcNow.Year.ToString());

            (string Subject, string? Html, string? Text) result = (EmailTemplates.PasswordResetSubject, html, default);

            return Task.FromResult(result);
        }

        if (templateKey == "EMAIL_CONFIRMATION")
        {
            var html = ReplaceTokens(EmailTemplates.EmailConfirmationHtml, tokens)
                .Replace("{YEAR}", DateTime.UtcNow.Year.ToString());

            (string Subject, string? Html, string? Text) result = (EmailTemplates.EmailConfirmationSubject, html, default);

            return Task.FromResult(result);
        }

        throw new InvalidOperationException($"Unknown email template: {templateKey}");
    }

    private static string ReplaceTokens(string template, Dictionary<string, string> tokens)
    {
        foreach (var token in tokens)
        {
            template = template.Replace(token.Key, token.Value, StringComparison.OrdinalIgnoreCase);
        }
        return template;
    }
}
