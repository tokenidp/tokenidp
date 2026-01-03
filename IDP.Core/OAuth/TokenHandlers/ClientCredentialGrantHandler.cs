using IDP.Core.Model;
using IDP.Core.OAuth.DomainServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IDP.Core.OAuth.TokenHandlers;

internal sealed class ClientCredentialGrantHandler : ITokenGrantHandler
{
    private const string GrantTypeValue = "client_credentials";

    private readonly ClientService _clientService;
    private readonly TokenService _tokenService;
    private readonly IAppLogger<ClientCredentialGrantHandler> _logger;

    public ClientCredentialGrantHandler(
        ClientService clientService,
        TokenService tokenService,
        IAppLogger<ClientCredentialGrantHandler> logger)
    {
        _clientService = clientService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        var normalizedRequest = NormalizeRequest(request);
        var client = await LoadClientAsync(normalizedRequest.ClientId);

        ValidateClientState(client, normalizedRequest.ClientId);
        ValidateClientSecret(normalizedRequest.ClientSecret, client.ClientSecret, normalizedRequest.ClientId);

        var scopes = ResolveScopes(normalizedRequest.Scope, client.Scopes, normalizedRequest.ClientId);
        var claims = BuildClientClaims(normalizedRequest.ClientId);

        return await IssueTokenAsync(client, claims, scopes, normalizedRequest.IpAddress);
    }

    private static NormalizedRequest NormalizeRequest(TokenRequest request)
    {
        if (request is null)
        {
            throw new GrantValidationException("invalid_request", "Request is missing.");
        }

        if (!string.Equals(request.GrantType, GrantTypeValue, StringComparison.Ordinal))
        {
            throw new GrantValidationException("unsupported_grant_type", "Invalid grant_type.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new GrantValidationException("invalid_request", "client_id is required.");
        }

        return new NormalizedRequest(
            request.ClientId.Trim(),
            request.Scope,
            request.IpAddress,
            request.ClientSecret);
    }

    private async Task<dynamic> LoadClientAsync(string clientId)
    {
        // TODO: replace with real client lookup (database/cache/service).
        var client = await _clientService.GetClient(clientId);
        if (client is null)
        {
            LogInvalidClient(clientId, "not found");
            throw new GrantValidationException("invalid_client", "Client not found.");
        }

        return client;
    }

    private void ValidateClientState(dynamic client, string clientId)
    {
        if (!client.IsActive)
        {
            LogInvalidClient(clientId, "inactive");
            throw new GrantValidationException("invalid_client", "Client is inactive.");
        }
    }

    private void ValidateClientSecret(string? providedSecret, string? storedSecret, string clientId)
    {
        if (!ValidateClientSecret(providedSecret, storedSecret))
        {
            LogInvalidClient(clientId, "secret mismatch");
            throw new GrantValidationException("invalid_client", "Client secret is invalid.");
        }
    }

    private HashSet<string> ResolveScopes(string? requestedScope, string clientScopes, string clientId)
    {
        var requestedScopes = ParseScopes(requestedScope);
        var allowedScopes = ParseScopes(clientScopes);

        if (requestedScopes.Count == 0)
        {
            requestedScopes = allowedScopes;
        }

        var invalidScopes = requestedScopes
            .Except(allowedScopes, StringComparer.Ordinal)
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            _logger.LogWarning("Invalid scopes requested for client {ClientId}: {Scopes}", clientId, invalidScopes);
            throw new GrantValidationException("invalid_scope", "One or more scopes are not allowed.");
        }

        return requestedScopes;
    }

    private static List<Claim> BuildClientClaims(string clientId)
    {
        return new List<Claim>
        {
            new("sub", clientId),
            new("client_id", clientId)
        };
    }

    private async Task<TokenResponse> IssueTokenAsync(
        dynamic client,
        List<Claim> claims,
        HashSet<string> scopes,
        string? ipAddress)
    {
        var scopeString = string.Join(' ', scopes);

        // TODO: issue token using your real token service.
        //return await _tokenService.CreateClientCredentialTokenAsync(
        //    client,
        //    claims,
        //    scopeString,
        //    ipAddress);
        return await Task.FromResult(default(TokenResponse));
    }

    private static HashSet<string> ParseScopes(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return scope
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool ValidateClientSecret(string? providedSecret, string? storedSecret)
    {
        if (string.IsNullOrWhiteSpace(providedSecret) || string.IsNullOrWhiteSpace(storedSecret))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        var storedBytes = Encoding.UTF8.GetBytes(storedSecret);

        return providedBytes.Length == storedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(providedBytes, storedBytes);
    }

    private void LogInvalidClient(string clientId, string reason)
    {
        _logger.LogWarning("Client {ClientId} rejected: {Reason}.", clientId, reason);
    }

    private sealed record NormalizedRequest(
        string ClientId,
        string? Scope,
        string? IpAddress,
        string? ClientSecret);

    public sealed class GrantValidationException : Exception
    {
        public GrantValidationException(string error, string description)
            : base(description)
        {
            Error = error;
        }

        public string Error { get; }
    }
}