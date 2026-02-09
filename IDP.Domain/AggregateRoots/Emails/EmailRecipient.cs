namespace IDP.Domain.AggregateRoots.Emails;

public sealed class EmailRecipient
{
    public long Id { get; private set; }
    public long EmailMessageId { get; private set; }

    public RecipientType RecipientType { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }

    private EmailRecipient() { }

    public static EmailRecipient Create(RecipientType type, string address, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address required.");

        return new EmailRecipient
        {
            RecipientType = type,
            Address = address.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()
        };
    }
}
