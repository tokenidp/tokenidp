namespace IDP.Domain.AggregateRoots.Clients;

public sealed class ClientExpiringSecret
{
    public int ExpiringClientCount { get; init; }
    public IReadOnlyList<ClientExpiringSecretItem> Clients { get; init; } = [];
}

public sealed class ClientExpiringSecretItem
{
    public int ClientId { get; init; }
    public string ClientName { get; init; } = default!;
    public DateTime ExpiresAtUtc { get; init; }
    public int DaysLeft { get; init; }
}