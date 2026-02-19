namespace IDP.Infrastructure.Emails.Abstractions;

public interface IEmailTemplateRenderer
{
    Task<(string Subject, string? Html, string? Text)> RenderAsync(
       int tenantId,
       string templateKey,
       string? modelJson,
       CancellationToken ct);
}
