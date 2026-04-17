using FluentAssertions;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.Clients;

public class ClientSecretTests
{
    [Fact]
    public void AddSecret_ShouldIgnoreDuplicateSecretHash()
    {
        var createResult = Client.Create(
            tenantId: 1,
            clientId: "client-1",
            clientName: "Test Client",
            description: null,
            appType: ClientTypes.WebApp,
            tokenType: TokenTypes.JWT,
            redirectUri: "https://app.example.com/callback",
            logoutRedirectUri: "https://app.example.com/logout",
            isActive: true,
            clientSecretExpiry: 30,
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 30,
            permitLimit: null,
            timeWindow: null,
            queueLimit: null,
            enableITracking: false,
            cibaEnabled: false,
            backchannelTokenDeliveryMode: CibaTokenDeliveryModes.Poll,
            cibaDefaultExpirySeconds: 300,
            cibaMinIntervalSeconds: 5,
            requireCibaUserCode: false,
            allowCibaLoginHint: true,
            allowCibaLoginHintToken: false,
            allowCibaIdTokenHint: false,
            out var client);

        createResult.IsSuccess.Should().BeTrue();
        client.Should().NotBeNull();

        var firstSecretResult = ClientSecret.Create(
            "secret-hash",
            "Initial secret",
            DateTime.UtcNow.AddDays(30),
            out var firstSecret);
        var duplicateSecretResult = ClientSecret.Create(
            "secret-hash",
            "Duplicate secret",
            DateTime.UtcNow.AddDays(30),
            out var duplicateSecret);

        firstSecretResult.IsSuccess.Should().BeTrue();
        duplicateSecretResult.IsSuccess.Should().BeTrue();

        client!.AddSecret(firstSecret!).IsSuccess.Should().BeTrue();
        client.AddSecret(duplicateSecret!).IsSuccess.Should().BeTrue();

        client.ClientSecrets.Should().HaveCount(1);
        client.ClientSecrets.Single().SecretHash.Should().Be("secret-hash");
    }
}
