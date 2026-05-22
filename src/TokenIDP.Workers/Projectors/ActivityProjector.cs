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
        if (!Enum.TryParse<ActivityEventType>(evt.EventType, out var activityType)
            && !IsKnownDomainEvent(evt.EventType))
        {
            _appLogger.LogWarning(
                "Unknown ActivityEventType: {EventType} for OutboxEvent {OutboxEventId}",
                evt.EventType, evt.Id);
        }

        var activity = evt.EventType switch
        {
            nameof(JwtTokenIssuedEvent) => OnTokenIssued(evt, ct),
            nameof(ReferenceTokenIssuedEvent) => OnReferenceIssued(evt, ct),
            nameof(RefreshTokenIssuedEvent) => OnRefreshIssued(evt, ct),
            nameof(TokenRevokedEvent) => OnTokenRevoked(evt, ct),
            nameof(TokenExpiredEvent) => OnTokenExpired(evt, ct),
            nameof(TokenRefreshRotatedEvent) => OnRefreshRotated(evt, ct),
            nameof(TokenRefreshReuseDetectedEvent) => OnRefreshReuseDetected(evt, ct),
            nameof(TenantCreatedEvent) => ProjectTenantActivity(evt, ActivityEventType.TenantCreated),
            nameof(TenantActivatedEvent) => ProjectTenantActivity(evt, ActivityEventType.TenantUpdated),
            nameof(TenantInactivatedEvent) => ProjectTenantActivity(evt, ActivityEventType.TenantDisabled),
            nameof(TenantBrandingChangedEvent) => ProjectTenantActivity(evt, ActivityEventType.TenantUpdated),
            nameof(TenantPlanChangedEvent) => ProjectTenantActivity(evt, ActivityEventType.TenantUpdated),
            _ when Enum.TryParse<ActivityEventType>(evt.EventType, out activityType)
                => ProjectActivityEvent(evt, activityType, ct),
            _ => null
        };

        if (activity is null)
        {
            throw new InvalidOperationException(
                $"No activity projection defined for outbox event {evt.EventType}");
        }

        _db.Activities.Add(activity);

        await _db.SaveChangesAsync(ct);
    }

    private static bool IsKnownDomainEvent(string eventType)
        => eventType is nameof(JwtTokenIssuedEvent)
            or nameof(ReferenceTokenIssuedEvent)
            or nameof(RefreshTokenIssuedEvent)
            or nameof(TokenRevokedEvent)
            or nameof(TokenExpiredEvent)
            or nameof(TokenRefreshRotatedEvent)
            or nameof(TokenRefreshReuseDetectedEvent)
            or nameof(TenantCreatedEvent)
            or nameof(TenantActivatedEvent)
            or nameof(TenantInactivatedEvent)
            or nameof(TenantBrandingChangedEvent)
            or nameof(TenantPlanChangedEvent);

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

    private Activity OnRefreshRotated(OutboxEvent evt, CancellationToken ct) =>
        ProjectTokenLifecycleActivity<TokenRefreshRotatedEvent>(evt, ActivityEventType.TokenRefreshIssue, "Rotated", "Refresh token rotated", ct);

    private Activity OnRefreshReuseDetected(OutboxEvent outboxEvent, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<TokenRefreshReuseDetectedEvent>(outboxEvent.PayloadJson)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {nameof(TokenRefreshReuseDetectedEvent)}");

        return Activity.Create(
            tenantId: outboxEvent.TenantId,
            category: ActivityCategory.SystemSecurity,
            eventType: ActivityEventType.SuspiciousLoginDetected,
            severity: ActivitySeverity.Alert,
            actorType: ActivityActorType.User,
            actorId: evt.UserId.ToString(),
            actorDisplayName: null,
            targetType: ActivityTargetType.Token,
            targetId: evt.TokenId.ToString(),
            targetDescription: "Refresh token reuse detected",
            status: "Detected",
            description: $"Refresh token reuse detected for client '{evt.ClientId}'.",
            correlationId: null,
            ipAddress: null,
            userAgent: null,
            outboxEventId: outboxEvent.Id);
    }

    private Activity? ProjectActivityEvent(OutboxEvent evt, ActivityEventType activityType, CancellationToken ct)
        => activityType switch
        {
            ActivityEventType.LoginSucceeded or
            ActivityEventType.LoginFailed or
            ActivityEventType.Logout or
            ActivityEventType.MfaChallengeSent or
            ActivityEventType.MfaValidated or
            ActivityEventType.MfaFailed or
            ActivityEventType.PasswordResetRequested or
            ActivityEventType.PasswordResetCompleted or
            ActivityEventType.AccountLocked or
            ActivityEventType.AccountUnlocked
                => ProjectAuthActivity<AuthenticationFlowEvent>(evt, ct),

            ActivityEventType.TenantCreated or
            ActivityEventType.TenantUpdated or
            ActivityEventType.TenantDisabled or
            ActivityEventType.SecurityPolicyChanged or
            ActivityEventType.MfaPolicyChanged or
            ActivityEventType.IpRestrictionChanged
                => ProjectTenantActivity(evt, activityType),

            _ => ProjectGenericActivity(evt, activityType)
        };

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

    private static Activity ProjectGenericActivity(OutboxEvent outboxEvent, ActivityEventType eventType)
    {
        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        var root = payload.RootElement;
        var actorId = GetStringProperty(root, "ActorId")
            ?? GetStringProperty(root, "UserId")
            ?? GetStringProperty(root, "ClientId");
        var targetId = GetStringProperty(root, "TargetId")
            ?? GetStringProperty(root, "TokenId")
            ?? GetStringProperty(root, "ClientId")
            ?? GetStringProperty(root, "UserId")
            ?? outboxEvent.AggregateId;
        var description = GetStringProperty(root, "Description")
            ?? ResolveGenericDescription(eventType, targetId);
        var status = GetStringProperty(root, "Status")
            ?? ResolveGenericStatus(eventType);

        return Activity.Create(
            tenantId: outboxEvent.TenantId,
            category: ResolveCategory(eventType),
            eventType: eventType,
            severity: ResolveSeverity(eventType),
            actorType: ResolveActorType(eventType),
            actorId: actorId,
            actorDisplayName: GetStringProperty(root, "ActorDisplayName"),
            targetType: ResolveTargetType(eventType),
            targetId: targetId,
            targetDescription: GetStringProperty(root, "TargetDescription") ?? targetId,
            status: status,
            description: description,
            correlationId: GetGuidProperty(root, "CorrelationId"),
            ipAddress: GetStringProperty(root, "IpAddress"),
            userAgent: GetStringProperty(root, "UserAgent"),
            outboxEventId: outboxEvent.Id);
    }

    private static string ResolveTenantStatus(ActivityEventType eventType)
        => eventType switch
        {
            ActivityEventType.TenantCreated => "Created",
            ActivityEventType.TenantDisabled => "Disabled",
            ActivityEventType.SecurityPolicyChanged => "Changed",
            ActivityEventType.MfaPolicyChanged => "Changed",
            ActivityEventType.IpRestrictionChanged => "Changed",
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
            ActivityEventType.SecurityPolicyChanged => $"{tenantLabel} security policy changed.",
            ActivityEventType.MfaPolicyChanged => $"{tenantLabel} MFA policy changed.",
            ActivityEventType.IpRestrictionChanged => $"{tenantLabel} IP restriction changed.",
            ActivityEventType.TenantUpdated when payload.TryGetProperty("PlanCode", out var planCode)
                => $"{tenantLabel} plan changed to '{planCode.GetString()}'.",
            ActivityEventType.TenantUpdated when payload.TryGetProperty("IsSystemTenant", out _)
                => $"{tenantLabel} activated.",
            ActivityEventType.TenantUpdated => $"{tenantLabel} branding updated.",
            _ => $"{tenantLabel} updated."
        };
    }

    private static string? GetStringProperty(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static Guid? GetGuidProperty(JsonElement payload, string propertyName)
        => payload.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var guid)
                ? guid
                : null;

    private static ActivityCategory ResolveCategory(ActivityEventType eventType)
        => eventType switch
        {
            >= ActivityEventType.LoginSucceeded and <= ActivityEventType.AccountUnlocked => ActivityCategory.Authentication,
            >= ActivityEventType.TokenJWTIssued and <= ActivityEventType.TokenReferenceIssue => ActivityCategory.Authorization,
            >= ActivityEventType.UserCreated and <= ActivityEventType.PermissionRevoked => ActivityCategory.UserManagement,
            >= ActivityEventType.ClientCreated and <= ActivityEventType.GrantTypeChanged => ActivityCategory.ClientManagement,
            >= ActivityEventType.TenantCreated and <= ActivityEventType.IpRestrictionChanged => ActivityCategory.TenantManagement,
            >= ActivityEventType.SuspiciousLoginDetected and <= ActivityEventType.CertificateExpired => ActivityCategory.SystemSecurity,
            _ => ActivityCategory.SystemSecurity
        };

    private static ActivitySeverity ResolveSeverity(ActivityEventType eventType)
        => eventType switch
        {
            ActivityEventType.LoginFailed or
            ActivityEventType.MfaFailed or
            ActivityEventType.ScopeDenied or
            ActivityEventType.OutboxRetry => ActivitySeverity.Warning,

            ActivityEventType.AccountLocked or
            ActivityEventType.SuspiciousLoginDetected or
            ActivityEventType.BruteForceDetected or
            ActivityEventType.RateLimitTriggered or
            ActivityEventType.OutboxFailed or
            ActivityEventType.BackgroundJobFailed or
            ActivityEventType.CertificateExpired => ActivitySeverity.Alert,

            ActivityEventType.UserDeleted or
            ActivityEventType.UserDisabled or
            ActivityEventType.ClientDisabled or
            ActivityEventType.TenantDisabled or
            ActivityEventType.TokenRevoked or
            ActivityEventType.TokenExpired or
            ActivityEventType.ConsentRevoked => ActivitySeverity.Warning,

            ActivityEventType.LoginSucceeded or
            ActivityEventType.MfaValidated or
            ActivityEventType.PasswordResetCompleted or
            ActivityEventType.AccountUnlocked or
            ActivityEventType.ConsentGranted or
            ActivityEventType.UserCreated or
            ActivityEventType.UserEnabled or
            ActivityEventType.ClientCreated or
            ActivityEventType.ClientEnabled or
            ActivityEventType.TenantCreated => ActivitySeverity.Success,

            _ => ActivitySeverity.Info
        };

    private static ActivityActorType ResolveActorType(ActivityEventType eventType)
        => ResolveCategory(eventType) switch
        {
            ActivityCategory.ClientManagement => ActivityActorType.Admin,
            ActivityCategory.TenantManagement => ActivityActorType.System,
            ActivityCategory.SystemSecurity => ActivityActorType.System,
            _ => ActivityActorType.User
        };

    private static ActivityTargetType? ResolveTargetType(ActivityEventType eventType)
        => eventType switch
        {
            >= ActivityEventType.TokenJWTIssued and <= ActivityEventType.TokenReferenceIssue => ActivityTargetType.Token,
            >= ActivityEventType.UserCreated and <= ActivityEventType.UserEnabled => ActivityTargetType.User,
            ActivityEventType.RoleAssigned or ActivityEventType.RoleRemoved => ActivityTargetType.Role,
            ActivityEventType.PermissionGranted or ActivityEventType.PermissionRevoked => ActivityTargetType.Permission,
            >= ActivityEventType.ClientCreated and <= ActivityEventType.GrantTypeChanged => ActivityTargetType.Client,
            >= ActivityEventType.TenantCreated and <= ActivityEventType.IpRestrictionChanged => ActivityTargetType.Tenant,
            ActivityEventType.OutboxRetry or ActivityEventType.OutboxFailed or ActivityEventType.BackgroundJobFailed => ActivityTargetType.Job,
            _ => null
        };

    private static string ResolveGenericStatus(ActivityEventType eventType)
        => eventType switch
        {
            ActivityEventType.LoginFailed or
            ActivityEventType.MfaFailed or
            ActivityEventType.ScopeDenied or
            ActivityEventType.OutboxFailed or
            ActivityEventType.BackgroundJobFailed => "Failed",
            ActivityEventType.SuspiciousLoginDetected or
            ActivityEventType.BruteForceDetected or
            ActivityEventType.RateLimitTriggered or
            ActivityEventType.CertificateExpired => "Detected",
            ActivityEventType.TokenRevoked or
            ActivityEventType.ConsentRevoked or
            ActivityEventType.PermissionRevoked => "Revoked",
            ActivityEventType.TokenExpired => "Expired",
            ActivityEventType.UserDeleted => "Deleted",
            ActivityEventType.UserDisabled or
            ActivityEventType.ClientDisabled or
            ActivityEventType.TenantDisabled => "Disabled",
            ActivityEventType.UserEnabled or
            ActivityEventType.ClientEnabled or
            ActivityEventType.AccountUnlocked => "Enabled",
            ActivityEventType.UserCreated or
            ActivityEventType.ClientCreated or
            ActivityEventType.TenantCreated => "Created",
            ActivityEventType.UserUpdated or
            ActivityEventType.ClientUpdated or
            ActivityEventType.TenantUpdated or
            ActivityEventType.SecurityPolicyChanged or
            ActivityEventType.MfaPolicyChanged or
            ActivityEventType.IpRestrictionChanged or
            ActivityEventType.GrantTypeChanged => "Updated",
            _ => "Success"
        };

    private static string ResolveGenericDescription(ActivityEventType eventType, string? targetId)
    {
        var target = string.IsNullOrWhiteSpace(targetId)
            ? "target"
            : $"target '{targetId}'";

        return eventType switch
        {
            ActivityEventType.ScopeDenied => $"Scope request denied for {target}.",
            ActivityEventType.ConsentGranted => $"Consent granted for {target}.",
            ActivityEventType.ConsentRevoked => $"Consent revoked for {target}.",
            ActivityEventType.UserCreated => $"User {target} created.",
            ActivityEventType.UserUpdated => $"User {target} updated.",
            ActivityEventType.UserDeleted => $"User {target} deleted.",
            ActivityEventType.UserDisabled => $"User {target} disabled.",
            ActivityEventType.UserEnabled => $"User {target} enabled.",
            ActivityEventType.RoleAssigned => $"Role assigned to {target}.",
            ActivityEventType.RoleRemoved => $"Role removed from {target}.",
            ActivityEventType.PermissionGranted => $"Permission granted to {target}.",
            ActivityEventType.PermissionRevoked => $"Permission revoked from {target}.",
            ActivityEventType.ClientCreated => $"Client {target} created.",
            ActivityEventType.ClientUpdated => $"Client {target} updated.",
            ActivityEventType.ClientDisabled => $"Client {target} disabled.",
            ActivityEventType.ClientEnabled => $"Client {target} enabled.",
            ActivityEventType.ClientSecretRotated => $"Client secret rotated for {target}.",
            ActivityEventType.GrantTypeChanged => $"Grant type changed for {target}.",
            ActivityEventType.SuspiciousLoginDetected => $"Suspicious login detected for {target}.",
            ActivityEventType.BruteForceDetected => $"Brute force attempt detected for {target}.",
            ActivityEventType.RateLimitTriggered => $"Rate limit triggered for {target}.",
            ActivityEventType.OutboxRetry => $"Outbox retry scheduled for {target}.",
            ActivityEventType.OutboxFailed => $"Outbox processing failed for {target}.",
            ActivityEventType.BackgroundJobFailed => $"Background job failed for {target}.",
            ActivityEventType.SigningKeyRotated => "Signing key rotated.",
            ActivityEventType.CertificateExpired => $"Certificate expired for {target}.",
            _ => $"{eventType} activity recorded for {target}."
        };
    }

    private static Guid GetTargetId(object evt) => evt switch
    {
        JwtTokenIssuedEvent e => e.TokenId,
        RefreshTokenIssuedEvent e => e.TokenId,
        ReferenceTokenIssuedEvent e => e.TokenId,
        TokenRevokedEvent e => e.TokenId,
        TokenExpiredEvent e => e.TokenId,
        TokenRefreshRotatedEvent e => e.NewRefreshTokenId,
        TokenRefreshReuseDetectedEvent e => e.TokenId,
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
        TokenRefreshRotatedEvent e => e.UserId,
        TokenRefreshReuseDetectedEvent e => e.UserId,
        _ => throw new InvalidOperationException(
            $"Unsupported event type {evt.GetType().Name}")
    };
}

