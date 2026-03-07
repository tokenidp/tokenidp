using IDP.Core.Model;
using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Tenants;
using IDP.ExternalProviders.Abstractions;
using IDP.ExternalProviders.Model;
using IDP.ExternalProviders.Security;
using IDP.Foundation.Abstractions;
using IDP.Foundation.Abstractions.Stores;
using Microsoft.Extensions.Options;

namespace IDP.ExternalProviders;

public sealed class ExternalAuthUseCase : IExternalAuthUseCase
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExternalProviderFactory _externalProviderFactory;
    private readonly IExternalAuthSessionStore _externalAuthSessionStore;
    private readonly IExternalIdentityLinkService _externalIdentityLinkService;
    private readonly IUserSignInService _userSignInService;
    private readonly IAuthorizationStore _authorizationStore;
    private readonly ExternalAuthOptions _options;

    public ExternalAuthUseCase(
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserService currentUserService,
        IExternalProviderFactory externalProviderFactory,
        IExternalAuthSessionStore externalAuthSessionStore,
        IExternalIdentityLinkService externalIdentityLinkService,
        IUserSignInService userSignInService,
        IOptions<ExternalAuthOptions> options,
        IAuthorizationStore authorizationStore)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _currentUserService = currentUserService;
        _externalProviderFactory = externalProviderFactory;
        _externalAuthSessionStore = externalAuthSessionStore;
        _externalIdentityLinkService = externalIdentityLinkService;
        _userSignInService = userSignInService;
        _options = options.Value;
        _authorizationStore = authorizationStore;
    }

    public async Task<ExternalChallengeResult> StartChallengeAsync(
        ExternalProviderTypes provider,
        string authorizationContextId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(authorizationContextId))
        {
            throw new InvalidOperationException("Authorization context id is required.");
        }

        var preAuthorization = await _authorizationStore
                   .GetPreAuthorization(authorizationContextId);

        if (preAuthorization is null)
        {
            throw new InvalidOperationException("Authorization context is invalid or expired.");
        }

        var tenantId = preAuthorization.TenantId;
        var clientId = preAuthorization.ClientId_FK;

        _tenantContextAccessor.SetClientId(clientId);
        _tenantContextAccessor.SetTenantId(tenantId);

        var state = OAuthStateGenerator.Generate();
        var nonce = NonceGenerator.Generate();
        var pkce = PkceGenerator.Generate();
        var callbackUrl = BuildProviderCallbackUrl(provider);

        var challengeRequest = new ExternalChallengeRequest(
            tenantId,
            callbackUrl,
            state,
            nonce,
            pkce.CodeVerifier);

        var session = new ExternalAuthSession(
            tenantId,
            clientId,
            authorizationContextId,
            provider,
            state,
            callbackUrl,
            DateTime.UtcNow,
            nonce,
            pkce.CodeVerifier);

        var ttl = TimeSpan.FromMinutes(Math.Max(1, _options.SessionTtlMinutes));

        await _externalAuthSessionStore.CreateAsync(session, ttl);

        _tenantContextAccessor.SetTenantId(tenantId);
        _tenantContextAccessor.SetClientId(clientId);

        var providerClient = _externalProviderFactory.Get(provider);
        var redirectUrl = providerClient.BuildAuthorizeUrl(challengeRequest);

        return new ExternalChallengeResult(redirectUrl);
    }

    public async Task<ExternalCallbackResult> HandleCallbackAsync(
        ExternalAuthCallbackInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            throw new InvalidOperationException("Authorization code is required.");
        }

        if (string.IsNullOrWhiteSpace(input.State))
        {
            throw new InvalidOperationException("State is required.");
        }

        var session = await _externalAuthSessionStore.GetAsync(input.Provider, input.State);

        if (session is null)
        {
            throw new InvalidOperationException("External authentication session was not found or expired.");
        }

        if (!string.Equals(session.State, input.State, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid state parameter.");
        }

        if (session.TenantId == 0)
        {
            throw new InvalidOperationException("Invalid tenant context in external callback.");
        }

        if (session.ClientId == 0)
        {
            throw new InvalidOperationException("Invalid client context in external callback.");
        }

        try
        {
            _tenantContextAccessor.SetTenantId(session.TenantId);
            _tenantContextAccessor.SetClientId(session.ClientId);

            var providerClient = _externalProviderFactory.Get(input.Provider);

            var callbackRequest = new ExternalCallbackRequest(
                session.TenantId,
                session.CallbackUrl,
                input.Code,
                input.State,
                session.CodeVerifier);

            var tokens = await providerClient.ExchangeCodeAsync(callbackRequest, cancellationToken);
            var identity = await providerClient.GetIdentityAsync(tokens, cancellationToken);

            var user = await _externalIdentityLinkService.FindOrProvisionUserAsync(
                session.TenantId,
                session.ClientId,
                identity,
                cancellationToken);

            await _userSignInService.SignInAsync(user, session.TenantId, cancellationToken);

            var resumeUrl = BuildResumeAuthorizeUrl(session.AuthorizationContextId);

            return new ExternalCallbackResult(resumeUrl);
        }
        finally
        {
            await _externalAuthSessionStore.RemoveAsync(input.Provider, input.State);
            _tenantContextAccessor.Clear();
        }
    }

    private string BuildProviderCallbackUrl(ExternalProviderTypes provider)
    {
        var baseUrl = _currentUserService.BaseUrl;
        return $"{baseUrl}/external/{provider.ToString().ToLowerInvariant()}/callback";
    }

    private string BuildResumeAuthorizeUrl(string authorizationContextId)
    {
        var baseUrl = _currentUserService.BaseUrl;
        return $"{baseUrl}/authorize?ctx={Uri.EscapeDataString(authorizationContextId)}";
    }
}
