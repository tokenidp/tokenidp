namespace IDP.Domain.AggregateRoots.Emails;

public sealed class EmailAttachment
{
    public long Id { get; private set; }
    public long EmailMessageId { get; private set; }

    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long SizeBytes { get; private set; }

    public byte StorageMode { get; private set; } // 0 Inline, 1 BlobRef
    public byte[]? Content { get; private set; }
    public string? BlobPath { get; private set; }

    public EmailMessage EmailMessage { get; private set; } = default!;

    private EmailAttachment() { }

    public static EmailAttachment Inline(string fileName, string contentType, byte[] content)
    {
        if (content is null || content.Length == 0) throw new ArgumentException("Content required.");

        return new EmailAttachment
        {
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = content.Length,
            StorageMode = 0,
            Content = content
        };
    }

    public static EmailAttachment BlobRef(string fileName, string contentType, long sizeBytes, string blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath)) throw new ArgumentException("BlobPath required.");

        return new EmailAttachment
        {
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageMode = 1,
            BlobPath = blobPath.Trim()
        };
    }
}

