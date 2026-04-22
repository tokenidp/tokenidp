using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Foundation.Options;

namespace TokenIDP.Server.Multitenancy;

public sealed class TenantRequestResolver : ITenantRequestResolver
{
    private readonly TenantResolutionOptions _options;
    private readonly IHostEnvironment _environment;

    public TenantRequestResolver(
        IOptions<TenantResolutionOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public TenantRequestResolutionResult Resolve(HttpContext httpContext)
    {
        var hostResult = TenantHostParser.Resolve(httpContext.Request.Host.Host, _options);
        if (hostResult.Kind == TenantHostResolutionKind.Invalid)
        {
            return new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.InvalidHost,
                FailureReason: "invalid_host");
        }

        if (hostResult.Kind == TenantHostResolutionKind.Tenant)
        {
            return new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.Resolved,
                hostResult.TenantKey!,
                TenantResolutionSource.Host);
        }

        if (hostResult.Kind == TenantHostResolutionKind.Root &&
            TryGetDefaultTenant(out var defaultTenant))
        {
            return new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.Resolved,
                defaultTenant,
                TenantResolutionSource.Default);
        }

        if (_environment.IsProduction())
        {
            return new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.InvalidHost,
                FailureReason: "invalid_host");
        }

        if (_options.AllowQueryInStaging &&
            TryResolveFromQuery(httpContext, out var queryTenantResult))
        {
            return queryTenantResult;
        }

        if (_options.AllowHeaderInStaging &&
            TryResolveFromHeader(httpContext, out var headerTenantResult))
        {
            return headerTenantResult;
        }

        if (TryGetDefaultTenant(out defaultTenant))
        {
            return new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.Resolved,
                defaultTenant,
                TenantResolutionSource.Default);
        }

        return new TenantRequestResolutionResult(
            TenantRequestResolutionStatus.Missing,
            FailureReason: "tenant_unavailable");
    }

    private bool TryResolveFromQuery(
        HttpContext httpContext,
        out TenantRequestResolutionResult result)
    {
        result = default!;

        var rawValue = httpContext.Request.Query[_options.QueryParameterName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (!TenantKeyValidator.TryNormalize(rawValue, _options, out var tenantKey))
        {
            result = new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.InvalidTenantKey,
                FailureReason: "invalid_tenant_key");
            return true;
        }

        result = new TenantRequestResolutionResult(
            TenantRequestResolutionStatus.Resolved,
            tenantKey,
            TenantResolutionSource.Query);
        return true;
    }

    private bool TryResolveFromHeader(
        HttpContext httpContext,
        out TenantRequestResolutionResult result)
    {
        result = default!;

        var rawValue = httpContext.Request.Headers[_options.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (!TenantKeyValidator.TryNormalize(rawValue, _options, out var tenantKey))
        {
            result = new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.InvalidTenantKey,
                FailureReason: "invalid_tenant_key");
            return true;
        }

        result = new TenantRequestResolutionResult(
            TenantRequestResolutionStatus.Resolved,
            tenantKey,
            TenantResolutionSource.Header);
        return true;
    }

    private bool TryGetDefaultTenant(out string tenantKey)
        => TenantKeyValidator.TryNormalize(_options.DefaultTenant, _options, out tenantKey);
}
