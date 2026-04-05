namespace IDP.Domain.AggregateRoots.Emails.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length == 0)
            throw new ArgumentException("Email address is required.");

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            Value = addr.Address;
        }
        catch
        {
            throw new ArgumentException("Invalid email address format.");
        }
    }

    public override string ToString() => Value;
}

public sealed record EmailRecipient(EmailAddress Address, string? DisplayName);

public sealed record EmailTemplateRef(string TemplateKey, string? ModelJson);

