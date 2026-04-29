using TokenIDP.Core.Abstractions;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Workers.Projectors;

internal class ActivityProjector
{
    private readonly ApplicationDbContext _db;
    private IAppLogger<ActivityProjector> _appLogger;

    public ActivityProjector(ApplicationDbContext db,
        IAppLogger<ActivityProjector> appLogger)
    {
        _db = db;
        _appLogger = appLogger;
    }

    public async Task ProjectAsync(OutboxEvent evt, CancellationToken ct)
    {
        if (!Enum.TryParse<ActivityEventType>(evt.EventType, out var activityType))
        {
            _appLogger.LogWarning(
                "Unknown ActivityEventType: {EventType} for OutboxEvent {OutboxEventId}",
                evt.EventType, evt.Id);
        }

        Activity? activity = null;

        switch (activityType)
        {
            case ActivityEventType.LoginSucceeded:
            case ActivityEventType.LoginFailed:
            case ActivityEventType.Logout:
            case ActivityEventType.MfaChallengeSent:
            case ActivityEventType.MfaValidated:
            case ActivityEventType.MfaFailed:
            case ActivityEventType.PasswordResetRequested:
            case ActivityEventType.PasswordResetCompleted:
            case ActivityEventType.AccountLocked:
            case ActivityEventType.AccountUnlocked:
                activity = ProjectAuthActivity<AuthenticationFlowEvent>(evt, ct);
                if (activity is null)
                {
                    return;
                }
                break;
            case ActivityEventType.TenantCreated:
            case ActivityEventType.TenantUpdated:
            case ActivityEventType.TenantDisabled:
                activity = ProjectTenantActivity(evt, activityType);
                break;

            default:
                break;
        }

        switch (evt.EventType)
        {
            case nameof(JwtTokenIssuedEvent):
                activity = OnTokenIssued(evt, ct);
                break;
            case nameof(ReferenceTokenIssuedEvent):
                activity = OnReferenceIssued(evt, ct);
                break;
            case nameof(RefreshTokenIssuedEvent):
                activity = OnRefreshIssued(evt, ct);
                break;
            case nameof(TokenRevokedEvent):
                activity = OnTokenRevoked(evt, ct);
                break;
            case nameof(TokenExpiredEvent):
                activity = OnTokenExpired(evt, ct);
                break;
            default:
                break;
        }

        if (activity is null)
        {
            throw new InvalidOperationException(
                $"No activity projection defined for outbox event {evt.EventType}");
        }

        _db.Activities.Add(activity);

        await _db.SaveChangesAsync(ct);
    }

    private Activity OnTokenIssued(OutboxEvent evt, CancellationToken ct) =>
       ProjectTokenActivity<JwtTokenIssuedEvent>(evt, ActivityEventType.TokenJWTIssued, ct);

    private Activity OnRefreshIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenActivity<RefreshTokenIssuedEvent>(evt, ActivityEventType.TokenRefreshIssue, ct);

    private Activity OnReferenceIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenActivity<ReferenceTokenIssuedEvent>(evt, ActivityEventType.TokenReferenceIssue, ct);

    private Activity OnTokenRevoked(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenLifecycleActivity<TokenRevokedEvent>(evt, ActivityEventType.TokenRevoked, "Revoked", "Token revoked", ct);

    private Activity OnTokenExpired(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenLifecycleActivity<TokenExpiredEvent>(evt, ActivityEventType.TokenExpired, "Expired", "Token expired", ct);

    private Activity? ProjectAuthActivity<TEvent>(OutboxEvent outboxEvent, CancellationToken ct)
        where TEvent : class
    {
        var evt = JsonSerializer.Deserialize<TEvent>(outboxEvent.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(TEvent).Name}");

        return evt switch
        {
            AuthenticationFlowEvent e => ProjectAuthenticationFlowActivity(outboxEvent, e),
            _ => throw new InvalidOperationException(
            $"No activity mapping defined for {evt.GetType().Name}")
        };
    }

    private Activity? ProjectAuthenticationFlowActivity(
        OutboxEvent outboxEvent,
        AuthenticationFlowEvent evt)
    {
        var tenantId = outboxEvent.TenantId > 0
            ? outboxEvent.TenantId
            : evt.TenantId;

        if (tenantId <= 0)
        {
            _appLogger.LogWarning(
                "Skipping auth activity projection for OutboxEvent {OutboxEventId} because TenantId is invalid. OutboxTenantId={OutboxTenantId}, PayloadTenantId={PayloadTenantId}",
                outboxEvent.Id,
                outboxEvent.TenantId,
                evt.TenantId);

            return null;
        }

        return Activity.Create
        (
            tenantId: tenantId,
            category: ActivityCategory.Authentication,
            eventType: Enum.Parse<ActivityEventType>(outboxEvent.EventType),
            severity: ActivitySeverity.Info,
            actorType: ActivityActorType.User,
            actorId: evt.UserId?.ToString(),
            actorDisplayName: null,
            targetType: ActivityTargetType.User,
            targetId: evt.UserId?.ToString(),
            targetDescription: evt.Description,
            status: evt.Result.ToString(),
            description: evt.Description,
            correlationId: evt.CorrelationId,
            ipAddress: evt.IpAddress,
            userAgent: evt.UserAgent,
            outboxEventId: outboxEvent.Id
        );
    }

    private Activity ProjectTokenActivity<TEvent>(OutboxEvent outboxEvent, ActivityEventType eventType, CancellationToken ct)
      where TEvent : class
    {
        var evt = JsonSerializer.Deserialize<TEvent>(outboxEvent.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(TEvent).Name}");

        var activity = Activity.Create(
            tenantId: outboxEvent.TenantId,
            category: ActivityCategory.Authentication,
            eventType: eventType,
            severity: ActivitySeverity.Info,
            actorType: ActivityActorType.User,
            actorId: GetActorId(evt).ToString(),
            actorDisplayName: null,
            targetType: ActivityTargetType.Token,
            targetId: GetTargetId(evt).ToString(),
            targetDescription: "Token issued",
            status: "Success",
            description: "Token issued",
            correlationId: null,
            ipAddress: null,
            userAgent: null,
            outboxEventId: outboxEvent.Id);

        return activity;
    }

    private Activity ProjectTokenLifecycleActivity<TEvent>(
        OutboxEvent outboxEvent,
        ActivityEventType eventType,
        string status,
        string description,
        CancellationToken ct)
        where TEvent : class
    {
        var evt = JsonSerializer.Deserialize<TEvent>(outboxEvent.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(TEvent).Name}");

        return Activity.Create(
            tenantId: outboxEvent.TenantId,
            category: ActivityCategory.Authentication,
            eventType: eventType,
            severity: ActivitySeverity.Info,
            actorType: ActivityActorType.User,
            actorId: GetActorId(evt)?.ToString(),
            actorDisplayName: null,
            targetType: ActivityTargetType.Token,
            targetId: GetTargetId(evt).ToString(),
            targetDescription: description,
            status: status,
            description: description,
            correlationId: null,
            ipAddress: null,
            userAgent: null,
            outboxEventId: outboxEvent.Id);
    }

    private static Activity ProjectTenantActivity(OutboxEvent outboxEvent, ActivityEventType eventType)
    {
        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        var tenantKey = GetStringProperty(payload.RootElement, "TenantKey");

        return Activity.Create(
            tenantId: outboxEvent.TenantId,
            category: ActivityCategory.TenantManagement,
            eventType: eventType,
            severity: ActivitySeverity.Info,
            actorType: ActivityActorType.System,
            actorId: null,
            actorDisplayName: null,
            targetType: ActivityTargetType.Tenant,
            targetId: outboxEvent.AggregateId,
            targetDescription: tenantKey,
            status: ResolveTenantStatus(eventType),
            description: ResolveTenantDescription(payload.RootElement, tenantKey, eventType),
            correlationId: null,
            ipAddress: null,
            userAgent: null,
            outboxEventId: outboxEvent.Id);
    }

    private static string ResolveTenantStatus(ActivityEventType eventType)
        => eventType switch
        {
            ActivityEventType.TenantCreated => "Created",
            ActivityEventType.TenantDisabled => "Disabled",
            ActivityEventType.TenantUpdated => "Updated",
            _ => "Updated"
        };

    private static string ResolveTenantDescription(JsonElement payload, string? tenantKey, ActivityEventType eventType)
    {
        var tenantLabel = string.IsNullOrWhiteSpace(tenantKey)
            ? "Tenant"
            : $"Tenant '{tenantKey}'";

        return eventType switch
        {
            ActivityEventType.TenantCreated => $"{tenantLabel} created.",
            ActivityEventType.TenantDisabled => $"{tenantLabel} disabled.",
            ActivityEventType.TenantUpdated when payload.TryGetProperty("PlanCode", out var planCode)
                => $"{tenantLabel} plan changed to '{planCode.GetString()}'.",
            ActivityEventType.TenantUpdated when payload.TryGetProperty("IsSystemTenant", out _)
                => $"{tenantLabel} activated.",
            ActivityEventType.TenantUpdated => $"{tenantLabel} branding updated.",
            _ => $"{tenantLabel} updated."
        };
    }

    private static string? GetStringProperty(JsonElement payload, string propertyName)
        => payload.TryGetProperty(propertyName, out var value)
            ? value.GetString()
            : null;

    private static Guid GetTargetId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.TokenId,
        RefreshTokenIssuedEvent e => e.TokenId,
        ReferenceTokenIssuedEvent e => e.TokenId,
        TokenRevokedEvent e => e.TokenId,
        TokenExpiredEvent e => e.TokenId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };

    private static long? GetActorId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.UserId,
        RefreshTokenIssuedEvent e => e.UserId,
        ReferenceTokenIssuedEvent e => e.UserId,
        TokenRevokedEvent e => e.UserId,
        TokenExpiredEvent e => e.UserId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };
}

