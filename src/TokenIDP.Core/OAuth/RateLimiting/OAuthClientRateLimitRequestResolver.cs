using System.Text;
using System.Text.Json;

namespace TokenIDP.Core.OAuth.RateLimiting;

internal interface IOAuthClientRateLimitRequestResolver
{
    ValueTask<OAuthClientRateLimitRequestContext> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}

internal sealed class OAuthClientRateLimitRequestResolver : IOAuthClientRateLimitRequestResolver
{
    private const string ResolverCacheKey = "__oauth_rate_limit_request_context";
    private static readonly HashSet<string> ProtectedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/authorize",
        "/token",
        "/device_authorization",
        "/introspect",
        "/revoke"
    };

    private readonly IClientRateLimitPolicyStore _policyStore;

    public OAuthClientRateLimitRequestResolver(IClientRateLimitPolicyStore policyStore)
    {
        _policyStore = policyStore;
    }

    public async ValueTask<OAuthClientRateLimitRequestContext> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (httpContext.Items.TryGetValue(ResolverCacheKey, out var cachedContext) &&
            cachedContext is Task<OAuthClientRateLimitRequestContext> cachedTask)
        {
            return await cachedTask;
        }

        var resolutionTask = ResolveCoreAsync(httpContext, cancellationToken).AsTask();
        httpContext.Items[ResolverCacheKey] = resolutionTask;

        return await resolutionTask;
    }

    private async ValueTask<OAuthClientRateLimitRequestContext> ResolveCoreAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var request = httpContext.Request;
        var endpoint = request.Path.Value ?? string.Empty;

        if (!ProtectedPaths.Contains(endpoint))
        {
            return OAuthClientRateLimitRequestContext.Bypass(endpoint);
        }

        var ipAddress = ResolveIpAddress(httpContext);
        var clientId = await ExtractClientIdAsync(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return OAuthClientRateLimitRequestContext.FromIpFallback(endpoint, ipAddress);
        }

        var profile = await _policyStore.GetAsync(clientId, cancellationToken);
        if (profile == null)
        {
            return OAuthClientRateLimitRequestContext.FromIpFallback(endpoint, ipAddress);
        }

        return OAuthClientRateLimitRequestContext.FromClientProfile(endpoint, ipAddress, profile);
    }

    private static string ResolveIpAddress(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    private static async ValueTask<string?> ExtractClientIdAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var path = request.Path.Value ?? string.Empty;

        if (path.Equals("/authorize", StringComparison.OrdinalIgnoreCase))
        {
            return TrimToNull(request.Query["client_id"].ToString());
        }

        if (path.Equals("/token", StringComparison.OrdinalIgnoreCase))
        {
            var bodyClientId = await ReadNamedValueAsync(
                request,
                ["client_id", "clientId"],
                cancellationToken);

            return bodyClientId ?? TryReadBasicAuthClientId(request);
        }

        if (path.Equals("/device_authorization", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/introspect", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/revoke", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadNamedValueAsync(
                request,
                ["client_id", "clientId"],
                cancellationToken);
        }

        return null;
    }

    private static async ValueTask<string?> ReadNamedValueAsync(
        HttpRequest request,
        string[] keys,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        try
        {
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(cancellationToken);
                foreach (var key in keys)
                {
                    var value = TrimToNull(form[key].ToString());
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return null;
            }

            if (!request.Body.CanRead)
            {
                return null;
            }

            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (document.RootElement.TryGetProperty(key, out var property))
                {
                    return property.ValueKind switch
                    {
                        JsonValueKind.String => TrimToNull(property.GetString()),
                        JsonValueKind.Null => null,
                        _ => TrimToNull(property.ToString())
                    };
                }
            }

            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }

    private static string? TryReadBasicAuthClientId(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return null;
        }

        var rawValue = authorizationHeader.ToString();
        if (!rawValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var encodedCredentials = rawValue["Basic ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(encodedCredentials))
        {
            return null;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return null;
            }

            return TrimToNull(decoded[..separatorIndex]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal sealed record OAuthClientRateLimitRequestContext(
    string Endpoint,
    string PartitionKey,
    string IpAddress,
    string? ClientId,
    int? TenantId,
    int PermitLimit,
    int QueueLimit,
    TimeSpan TimeWindow,
    bool ShouldRateLimit)
{
    public const int DefaultPermitLimit = 20;
    public const int DefaultQueueLimit = 0;
    public static readonly TimeSpan DefaultTimeWindow = TimeSpan.FromSeconds(60);

    public static OAuthClientRateLimitRequestContext Bypass(string endpoint)
    {
        return new OAuthClientRateLimitRequestContext(
            endpoint,
            "bypass",
            "unknown",
            null,
            null,
            DefaultPermitLimit,
            DefaultQueueLimit,
            DefaultTimeWindow,
            ShouldRateLimit: false);
    }

    public static OAuthClientRateLimitRequestContext FromIpFallback(string endpoint, string ipAddress)
    {
        return new OAuthClientRateLimitRequestContext(
            endpoint,
            $"ip:{ipAddress}",
            ipAddress,
            null,
            null,
            DefaultPermitLimit,
            DefaultQueueLimit,
            DefaultTimeWindow,
            ShouldRateLimit: true);
    }

    public static OAuthClientRateLimitRequestContext FromClientProfile(
        string endpoint,
        string ipAddress,
        ClientRateLimitProfile profile)
    {
        var permitLimit = profile.PermitLimit.GetValueOrDefault() > 0
            ? profile.PermitLimit!.Value
            : DefaultPermitLimit;

        var queueLimit = profile.QueueLimit.GetValueOrDefault() >= 0
            ? profile.QueueLimit.GetValueOrDefault(DefaultQueueLimit)
            : DefaultQueueLimit;

        var timeWindow = profile.TimeWindow.GetValueOrDefault() > TimeSpan.Zero
            ? profile.TimeWindow!.Value
            : DefaultTimeWindow;

        return new OAuthClientRateLimitRequestContext(
            endpoint,
            $"client:{profile.ClientId}",
            ipAddress,
            profile.ClientId,
            profile.TenantId,
            permitLimit,
            queueLimit,
            timeWindow,
            ShouldRateLimit: true);
    }
}
