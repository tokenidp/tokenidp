using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Security.Cryptography;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.AggregateRoots.Users;

namespace TokenIDP.Tests.OAuth;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "TokenIDP.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public int UserId { get; init; }
    public int TenantId { get; init; }
    public string TenantKey { get; init; } = string.Empty;
    public int AuthTenantId { get; init; }
    public string AuthTenantKey { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public string UserName { get; init; } = "tester";
    public string BaseUrl { get; init; } = "https://localhost";
    public string Scopes { get; init; } = "openid";
    public string? IpAddress { get; init; } = "127.0.0.1";
    public string? UserAgent { get; init; } = "TokenIDP.Tests";

    public string[] GetRoles() => Array.Empty<string>();
}

internal static class CibaTestData
{
    private static readonly Lazy<string> SigningKeyPem = new(() =>
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportRSAPrivateKeyPem();
    });

    public static TokenOptions CreateTokenOptions()
    {
        return new TokenOptions
        {
            Issuer = "https://issuer.test",
            Key = SigningKeyPem.Value
        };
    }

    public static ClientValidationSnapshot CreateClientSnapshot(
        string clientId = "ciba-client",
        int tenantId = 1,
        IEnumerable<GrantTypes>? grantTypes = null,
        IEnumerable<string>? scopes = null,
        bool cibaEnabled = true,
        int cibaDefaultExpirySeconds = 300,
        int cibaMinIntervalSeconds = 5,
        bool requireUserCode = false,
        bool allowLoginHint = true,
        bool allowLoginHintToken = false,
        bool allowIdTokenHint = false,
        TokenTypes tokenType = TokenTypes.JWT)
    {
        return new ClientValidationSnapshot(
            clientId,
            "CIBA Client",
            tenantId,
            isActive: true,
            redirectUri: "https://client.example/callback",
            logoutRedirectUri: "https://client.example/logout",
            clientType: ClientTypes.Backend,
            tokenType: tokenType,
            grantTypes: grantTypes ?? new[] { GrantTypes.ciba },
            scopes: scopes ?? new[] { StandardScopes.OpenId, StandardScopes.Profile },
            apiResources: Array.Empty<string>(),
            apiScopeAssignments: Array.Empty<ClientApiScopeAssignment>(),
            activeSecretHashes: new[] { "secret-hash" },
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 7,
            refreshTokenDeliveryMode: RefreshTokenDeliveryMode.Response,
            clientSecretExpiry: null,
            cibaEnabled: cibaEnabled,
            backchannelTokenDeliveryMode: CibaTokenDeliveryModes.Poll,
            cibaDefaultExpirySeconds: cibaDefaultExpirySeconds,
            cibaMinIntervalSeconds: cibaMinIntervalSeconds,
            requireCibaUserCode: requireUserCode,
            allowCibaLoginHint: allowLoginHint,
            allowCibaLoginHintToken: allowLoginHintToken,
            allowCibaIdTokenHint: allowIdTokenHint);
    }

    public static User CreateActiveUser(int tenantId = 1, int userId = 101, string email = "user@example.com")
    {
        var result = User.Create(
            tenantId,
            "Test",
            "User",
            "test.user",
            email,
            "555-0100",
            createdBy: 1,
            roles: new[] { 1 },
            out var user);

        result.IsSuccess.Should().BeTrue();
        user.Should().NotBeNull();

        SetProperty(user!, nameof(User.Id), userId);
        user!.GenerateUserCode(userId);
        user.ApplyIdentityFlags(
            lookoutEnabled: false,
            twoFactorEnabled: false,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            accessFailedCount: 0,
            lookoutEnd: null);

        return user!;
    }

    public static UserShortInfo CreateUserShortInfo(int userId = 101, int tenantId = 1)
    {
        return new UserShortInfo(
            userId,
            tenantId,
            "Test User",
            "user@example.com",
            emailConfirmed: true,
            userName: "test.user",
            firstName: "Test",
            lastName: "User",
            phoneNumber: "555-0100",
            phoneNumberVerified: true,
            createdOn: DateTime.UtcNow.AddDays(-1),
            updatedOn: null);
    }

    public static BackchannelAuthenticationRequest CreatePendingRequest(
        string rawAuthReqId = "raw-auth-req-id",
        int tenantId = 1,
        string clientId = "ciba-client",
        int userId = 101,
        string scopes = "openid profile",
        int intervalSeconds = 5,
        string? approvalToken = null)
    {
        var request = BackchannelAuthenticationRequest.Create(
            tenantId,
            clientId,
            userId,
            scopes,
            CibaUserHintType.LoginHint,
            "hint-hash",
            "u***@example.com",
            "12345",
            null,
            TokenIDP.Core.Foundation.Security.SecretHasher.HashSecret(rawAuthReqId),
            CibaDeliveryMode.Poll,
            requestedExpirySeconds: 300,
            expiresAtUtc: DateTime.UtcNow.AddMinutes(5),
            intervalSeconds: intervalSeconds,
            clientNotificationTokenHash: null,
            acrValues: null);

        if (!string.IsNullOrWhiteSpace(approvalToken))
        {
            request.SetApprovalChallenge(
                Guid.NewGuid(),
                TokenIDP.Core.Foundation.Security.SecretHasher.HashSecret(approvalToken),
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(5),
                "hint-hash");
        }

        return request;
    }

    public static void SetProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var property = typeof(TTarget).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property.Should().NotBeNull($"property {propertyName} should exist on {typeof(TTarget).Name}");
        property!.SetValue(target, value);
    }
}
