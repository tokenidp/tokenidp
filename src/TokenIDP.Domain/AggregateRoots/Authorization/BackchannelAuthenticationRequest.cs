namespace TokenIDP.Domain.AggregateRoots.Authorization;

public enum CibaDeliveryMode : byte
{
    Poll = 0,
    Ping = 1,
    Push = 2
}

public enum CibaUserHintType : byte
{
    LoginHint = 0,
    LoginHintToken = 1,
    IdTokenHint = 2
}

public enum CibaRequestStatus : byte
{
    Pending = 0,
    UserIdentified = 1,
    AwaitingAuthorization = 2,
    Approved = 3,
    Denied = 4,
    TokenIssued = 5,
    Expired = 6,
    Cancelled = 7,
    Failed = 8
}

public sealed class BackchannelAuthenticationRequest : AggregateRoot<int>, ITenant
{
    private BackchannelAuthenticationRequest()
    {
    }

    public int TenantId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public int? UserId { get; private set; }
    public string RequestedScopes { get; private set; } = default!;
    public CibaUserHintType HintType { get; private set; }
    public string HintValueHash { get; private set; } = default!;
    public string? SubjectHint { get; private set; }
    public string? BindingMessage { get; private set; }
    public string? UserCodeHash { get; private set; }
    public string AuthReqIdHash { get; private set; } = default!;
    public CibaRequestStatus Status { get; private set; }
    public CibaDeliveryMode DeliveryMode { get; private set; }
    public int? RequestedExpirySeconds { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int IntervalSeconds { get; private set; }
    public string? ClientNotificationTokenHash { get; private set; }
    public string? AcrValues { get; private set; }
    public string? ApprovedAcr { get; private set; }
    public string? ApprovedAmr { get; private set; }
    public string? DenialReason { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? DeniedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public DateTime? LastPolledAtUtc { get; private set; }
    public int PollCount { get; private set; }

    public static BackchannelAuthenticationRequest Create(
        int tenantId,
        string clientId,
        int userId,
        string requestedScopes,
        CibaUserHintType hintType,
        string hintValueHash,
        string? subjectHint,
        string? bindingMessage,
        string? userCodeHash,
        string authReqIdHash,
        CibaDeliveryMode deliveryMode,
        int? requestedExpirySeconds,
        DateTime expiresAtUtc,
        int intervalSeconds,
        string? clientNotificationTokenHash,
        string? acrValues)
    {
        if (tenantId <= 0)
            throw new DomainException("TenantId must be greater than zero.");

        if (userId <= 0)
            throw new DomainException("UserId must be greater than zero.");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("ClientId is required.");

        if (string.IsNullOrWhiteSpace(requestedScopes))
            throw new DomainException("Requested scopes are required.");

        if (string.IsNullOrWhiteSpace(hintValueHash))
            throw new DomainException("Hint hash is required.");

        if (string.IsNullOrWhiteSpace(authReqIdHash))
            throw new DomainException("auth_req_id hash is required.");

        if (intervalSeconds <= 0)
            throw new DomainException("Interval must be greater than zero.");

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("Expiration must be in the future.");

        return new BackchannelAuthenticationRequest
        {
            TenantId = tenantId,
            ClientId = clientId,
            UserId = userId,
            RequestedScopes = requestedScopes,
            HintType = hintType,
            HintValueHash = hintValueHash,
            SubjectHint = subjectHint,
            BindingMessage = bindingMessage,
            UserCodeHash = userCodeHash,
            AuthReqIdHash = authReqIdHash,
            Status = CibaRequestStatus.AwaitingAuthorization,
            DeliveryMode = deliveryMode,
            RequestedExpirySeconds = requestedExpirySeconds,
            ExpiresAtUtc = expiresAtUtc,
            IntervalSeconds = intervalSeconds,
            ClientNotificationTokenHash = clientNotificationTokenHash,
            AcrValues = acrValues
        };
    }

    public void Approve(string? acr = null, string? amr = null)
    {
        EnsureNotExpired();

        if (Status is not CibaRequestStatus.AwaitingAuthorization and not CibaRequestStatus.UserIdentified)
            throw new DomainException("Request cannot be approved in the current state.");

        Status = CibaRequestStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;
        ApprovedAcr = acr;
        ApprovedAmr = amr;
        DenialReason = null;
        DeniedAtUtc = null;
    }

    public void Deny(string? reason = null)
    {
        EnsureNotExpired();

        if (Status is not CibaRequestStatus.AwaitingAuthorization and not CibaRequestStatus.UserIdentified)
            throw new DomainException("Request cannot be denied in the current state.");

        Status = CibaRequestStatus.Denied;
        DenialReason = reason;
        DeniedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status is CibaRequestStatus.TokenIssued or CibaRequestStatus.Cancelled)
            throw new DomainException("Request cannot be cancelled in the current state.");

        Status = CibaRequestStatus.Cancelled;
        DenialReason = reason;
    }

    public void Fail(string? reason = null)
    {
        if (Status == CibaRequestStatus.TokenIssued)
            throw new DomainException("Issued requests cannot be failed.");

        Status = CibaRequestStatus.Failed;
        DenialReason = reason;
    }

    public void RegisterPoll()
    {
        EnsureNotExpired();

        if (DeliveryMode == CibaDeliveryMode.Push)
            throw new DomainException("unauthorized_client");

        var now = DateTime.UtcNow;

        if (LastPolledAtUtc.HasValue &&
            (now - LastPolledAtUtc.Value).TotalSeconds < IntervalSeconds)
        {
            IntervalSeconds += 5;
            LastPolledAtUtc = now;
            PollCount++;
            throw new DomainException("slow_down");
        }

        LastPolledAtUtc = now;
        PollCount++;

        switch (Status)
        {
            case CibaRequestStatus.AwaitingAuthorization:
            case CibaRequestStatus.UserIdentified:
            case CibaRequestStatus.Pending:
                throw new DomainException("authorization_pending");
            case CibaRequestStatus.Denied:
                throw new DomainException("access_denied");
            case CibaRequestStatus.Cancelled:
            case CibaRequestStatus.Failed:
                throw new DomainException("invalid_grant");
            case CibaRequestStatus.TokenIssued:
                throw new DomainException("invalid_grant");
            case CibaRequestStatus.Approved:
                return;
            default:
                throw new DomainException("invalid_grant");
        }
    }

    public void MarkTokenIssued()
    {
        EnsureNotExpired();

        if (Status != CibaRequestStatus.Approved)
            throw new DomainException("Request cannot be consumed in the current state.");

        Status = CibaRequestStatus.TokenIssued;
        ConsumedAtUtc = DateTime.UtcNow;
    }

    public void EnsureNotExpired()
    {
        if (DateTime.UtcNow <= ExpiresAtUtc)
            return;

        Status = CibaRequestStatus.Expired;
        throw new DomainException("expired_token");
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAtUtc;
    }
}
