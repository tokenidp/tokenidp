namespace IDP.Domain.AggregateRoots.Authorization;

public enum DeviceAuthorizationStatus : byte
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Consumed = 3,
    Expired = 4
}

public sealed class DeviceAuthorization : AggregateRoot<int>, ITenant
{
    private DeviceAuthorization() { }

    public int TenantId { get; private set; }
    public string ClientId { get; private set; } = default!;

    public string DeviceCodeHash { get; private set; } = default!;
    public string UserCodeHash { get; private set; } = default!;

    public string Scopes { get; private set; } = default!;

    public DateTime ExpiresAtUtc { get; private set; }
    public int IntervalSeconds { get; private set; }

    public DeviceAuthorizationStatus Status { get; private set; }

    public string? SubjectUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    public DateTime? LastPollUtc { get; private set; }
    public int PollCount { get; private set; }

    public string? CodeChallenge { get; private set; }
    public string? CodeChallengeMethod { get; private set; }

    public string? DeviceMetadata { get; private set; }

    #region Factory

    public static DeviceAuthorization Create(
        int tenantId,
        string clientId,
        string deviceCodeHash,
        string userCodeHash,
        string scopes,
        int expiresInSeconds,
        int intervalSeconds,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? deviceMetadata)
    {
        if (expiresInSeconds <= 0)
            throw new DomainException("Expiration must be positive.");

        if (intervalSeconds <= 0)
            throw new DomainException("Interval must be positive.");

        return new DeviceAuthorization
        {
            TenantId = tenantId,
            ClientId = clientId,
            DeviceCodeHash = deviceCodeHash,
            UserCodeHash = userCodeHash,
            Scopes = scopes,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds),
            IntervalSeconds = intervalSeconds,
            Status = DeviceAuthorizationStatus.Pending,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            DeviceMetadata = deviceMetadata
        };
    }

    #endregion

    #region Polling Logic

    public void RegisterPoll()
    {
        EnsureNotExpired();

        if (Status == DeviceAuthorizationStatus.Denied)
            throw new DomainException("access_denied");

        if (Status == DeviceAuthorizationStatus.Consumed)
            throw new DomainException("invalid_grant");

        var now = DateTime.UtcNow;

        if (LastPollUtc.HasValue &&
            (now - LastPollUtc.Value).TotalSeconds < IntervalSeconds)
        {
            throw new DomainException("slow_down");
        }

        LastPollUtc = now;
        PollCount++;

        if (Status == DeviceAuthorizationStatus.Pending)
            throw new DomainException("authorization_pending");

        if (Status == DeviceAuthorizationStatus.Approved)
            return;
    }

    #endregion

    #region Approval Logic

    public void Approve(string subjectUserId)
    {
        EnsureNotExpired();

        if (Status != DeviceAuthorizationStatus.Pending)
            throw new DomainException("Cannot approve in current state.");

        SubjectUserId = subjectUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        Status = DeviceAuthorizationStatus.Approved;
    }

    public void Deny()
    {
        EnsureNotExpired();

        if (Status != DeviceAuthorizationStatus.Pending)
            throw new DomainException("Cannot deny in current state.");

        Status = DeviceAuthorizationStatus.Denied;
    }

    #endregion

    #region Token Consumption

    public void MarkConsumed()
    {
        if (Status != DeviceAuthorizationStatus.Approved)
            throw new DomainException("Cannot consume unless approved.");

        Status = DeviceAuthorizationStatus.Consumed;
    }

    #endregion

    #region Expiry

    public void EnsureNotExpired()
    {
        if (DateTime.UtcNow > ExpiresAtUtc)
        {
            Status = DeviceAuthorizationStatus.Expired;
            throw new DomainException("expired_token");
        }
    }

    public bool IsExpired()
        => DateTime.UtcNow > ExpiresAtUtc;

    #endregion
}
