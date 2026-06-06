using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net.Http.Headers;
using System.Text;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Exceptions;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.Endpoints;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.OAuth;

public sealed class TokenEndpointClientAuthServiceTests
{
    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldAcceptClientSecretBasic()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("backend-client"))
            .ReturnsAsync(CreateClientSnapshot(
                clientId: "backend-client",
                clientType: ClientTypes.Backend,
                activeSecretHashes: new[] { SecretHasher.HashSecret("top-secret") }));

        var sut = CreateSut(clientRepository.Object);
        var context = CreateFormContext(("grant_type", "client_credentials"));
        context.Request.Headers.Authorization = Basic("backend-client", "top-secret");

        var request = await sut.BuildValidatedRequestAsync(context);

        request.ClientId.Should().Be("backend-client");
        request.ClientSecret.Should().Be("top-secret");
        request.ClientAuthenticationMethod.Should().Be(TokenEndpointAuthenticationMethods.ClientSecretBasic);
    }

    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldRejectMismatchedBodyClientId_WithBasicAuthentication()
    {
        var sut = CreateSut(Mock.Of<IClientRepository>());
        var context = CreateFormContext(
            ("grant_type", "client_credentials"),
            ("client_id", "other-client"));
        context.Request.Headers.Authorization = Basic("backend-client", "top-secret");

        var action = async () => await sut.BuildValidatedRequestAsync(context);

        var exception = await action.Should().ThrowAsync<TokenRequestValidationException>();
        exception.Which.Error.Should().Be("invalid_request");
        exception.Which.Message.Should().Be("client_id in the request body must match the Authorization header.");
    }

    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldAllowPublicAuthorizationCodeRequestWithoutSecret()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("spa-client"))
            .ReturnsAsync(CreateClientSnapshot(
                clientId: "spa-client",
                clientType: ClientTypes.SPA,
                activeSecretHashes: Array.Empty<string>()));

        var sut = CreateSut(clientRepository.Object);
        var context = CreateFormContext(
            ("grant_type", "authorization_code"),
            ("client_id", "spa-client"),
            ("code", "code-123"),
            ("code_verifier", "verifier-123"),
            ("redirect_uri", "https://app.example/callback"));

        var request = await sut.BuildValidatedRequestAsync(context);

        request.ClientId.Should().Be("spa-client");
        request.ClientAuthenticationMethod.Should().Be(TokenEndpointAuthenticationMethods.None);
        request.Code.Should().Be("code-123");
    }

    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldRequireAuthenticationForClientCredentials()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("backend-client"))
            .ReturnsAsync(CreateClientSnapshot(
                clientId: "backend-client",
                clientType: ClientTypes.Backend,
                activeSecretHashes: new[] { SecretHasher.HashSecret("top-secret") }));

        var sut = CreateSut(clientRepository.Object);
        var context = CreateFormContext(
            ("grant_type", "client_credentials"),
            ("client_id", "backend-client"));

        var action = async () => await sut.BuildValidatedRequestAsync(context);

        var exception = await action.Should().ThrowAsync<TokenRequestValidationException>();
        exception.Which.Error.Should().Be("invalid_client");
        exception.Which.Message.Should().Be("Client authentication is required.");
    }

    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldRejectClientFromDifferentOperationalTenant()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("tenant-20-client"))
            .ReturnsAsync(CreateClientSnapshot(
                clientId: "tenant-20-client",
                tenantId: 20,
                clientType: ClientTypes.Backend,
                activeSecretHashes: new[] { SecretHasher.HashSecret("top-secret") }));

        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TokenIDP.Core.Abstractions.TenantContext(
            TenantId: 10,
            TenantKey: "tenant-10",
            IsSystemTenant: false));

        var sut = CreateSut(clientRepository.Object, tenantContextAccessor);
        var context = CreateFormContext(("grant_type", "client_credentials"));
        context.Request.Headers.Authorization = Basic("tenant-20-client", "top-secret");

        var action = async () => await sut.BuildValidatedRequestAsync(context);

        var exception = await action.Should().ThrowAsync<TokenRequestValidationException>();
        exception.Which.Error.Should().Be("invalid_client");
        exception.Which.Message.Should().Be("Client authentication failed.");
    }

    [Fact]
    public async Task BuildValidatedRequestAsync_ShouldHideMissingClientDetails()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("missing-client"))
            .ThrowsAsync(new NotFoundException("Client not found."));

        var sut = CreateSut(clientRepository.Object);
        var context = CreateFormContext(
            ("grant_type", "authorization_code"),
            ("client_id", "missing-client"));

        var action = async () => await sut.BuildValidatedRequestAsync(context);

        var exception = await action.Should().ThrowAsync<TokenRequestValidationException>();
        exception.Which.Error.Should().Be("invalid_client");
        exception.Which.Message.Should().Be("Client authentication failed.");
    }

    private static TokenEndpointClientAuthService CreateSut(
        IClientRepository clientRepository,
        TenantContextAccessor? tenantContextAccessor = null)
    {
        return new TokenEndpointClientAuthService(
            clientRepository,
            Mock.Of<IAppLogger<TokenEndpointClientAuthService>>(),
            tenantContextAccessor ?? new TenantContextAccessor());
    }

    private static DefaultHttpContext CreateFormContext(params (string Key, string Value)[] values)
    {
        var body = string.Join("&", values.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.Body = new MemoryStream(bodyBytes);

        return context;
    }

    private static string Basic(string clientId, string secret)
    {
        var rawCredentials = Encoding.UTF8.GetBytes($"{clientId}:{secret}");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(rawCredentials)).ToString();
    }

    private static ClientValidationSnapshot CreateClientSnapshot(
        string clientId,
        int tenantId = 1,
        bool isSystemTenant = false,
        ClientTypes clientType = ClientTypes.Backend,
        IEnumerable<string>? activeSecretHashes = null)
    {
        return new ClientValidationSnapshot(
            clientId,
            "Test Client",
            tenantId,
            isSystemTenant,
            isActive: true,
            redirectUri: "https://app.example/callback",
            logoutRedirectUri: "https://app.example/logout",
            clientType,
            TokenTypes.JWT,
            new[] { GrantTypes.authorization_code, GrantTypes.client_credentials },
            new[] { StandardScopes.OpenId, StandardScopes.Profile },
            Array.Empty<string>(),
            Array.Empty<ClientApiScopeAssignment>(),
            activeSecretHashes ?? Array.Empty<string>(),
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 30,
            RefreshTokenDeliveryMode.Response,
            clientSecretExpiry: null,
            cibaEnabled: false,
            CibaTokenDeliveryModes.Poll,
            cibaDefaultExpirySeconds: 300,
            cibaMinIntervalSeconds: 5,
            requireCibaUserCode: false,
            allowCibaLoginHint: true,
            allowCibaLoginHintToken: false,
            allowCibaIdTokenHint: false);
    }
}
