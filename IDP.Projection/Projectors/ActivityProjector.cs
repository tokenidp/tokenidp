namespace IDP.Projection.Projectors;

internal class ActivityProjector
{
    private readonly IApplicationDbContext _db;
    private IAppLogger<ActivityProjector> _appLogger;

    public ActivityProjector(IApplicationDbContext db,
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

        Activity activity = default!;

        switch (activityType)
        {
            case ActivityEventType.LoginSucceeded:
            case ActivityEventType.LoginFailed:
            case ActivityEventType.MfaChallengeSent:
            case ActivityEventType.MfaValidated:
            case ActivityEventType.MfaFailed:
                activity = ProjectAuthActivity<AuthenticationFlowEvent>(evt, ct);
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
            case nameof(TokenExpiredEvent):
                break;
            default:
                break;
        }

        _db.Activities.Add(activity);

        await _db.SaveChangesAsync();
    }

    private Activity OnTokenIssued(OutboxEvent evt, CancellationToken ct) =>
       ProjectTokenActivity<JwtTokenIssuedEvent>(evt, ActivityEventType.TokenJWTIssued, ct);

    private Activity OnRefreshIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenActivity<RefreshTokenIssuedEvent>(evt, ActivityEventType.TokenRefreshIssue, ct);

    private Activity OnReferenceIssued(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenActivity<ReferenceTokenIssuedEvent>(evt, ActivityEventType.TokenReferenceIssue, ct);

    private Activity ProjectAuthActivity<TEvent>(OutboxEvent outboxEvent, CancellationToken ct)
        where TEvent : class
    {
        var evt = JsonSerializer.Deserialize<TEvent>(outboxEvent.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(TEvent).Name}");

        return evt switch
        {
            AuthenticationFlowEvent e =>
                Activity.Create
                (
                    tenantId: outboxEvent.TenantId,
                    category: ActivityCategory.Authentication,
                    eventType: Enum.Parse<ActivityEventType>(outboxEvent.EventType),
                    severity: ActivitySeverity.Info,
                    actorType: ActivityActorType.User,
                    actorId: e.UserId?.ToString(),
                    actorDisplayName: null,
                    targetType: ActivityTargetType.User,
                    targetId: e.UserId?.ToString(),
                    targetDescription: e.Description,
                    status: e.Result.ToString(),
                    description: e.Description,
                    correlationId: e.CorrelationId,
                    ipAddress: e.IpAddress,
                    userAgent: e.UserAgent,
                    outboxEventId: outboxEvent.Id
                ),
            _ => throw new InvalidOperationException(
            $"No activity mapping defined for {evt.GetType().Name}")
        };
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

    private static Guid GetTargetId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.TokenId,
        RefreshTokenIssuedEvent e => e.TokenId,
        ReferenceTokenIssuedEvent e => e.TokenId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };

    private static long? GetActorId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.UserId,
        RefreshTokenIssuedEvent e => e.UserId,
        ReferenceTokenIssuedEvent e => e.UserId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };
}
