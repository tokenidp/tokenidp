using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Clients.UseCases;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.Clients;

public class ClientCommandUseCaseTests
{
    [Fact]
    public async Task UpdateClient_ShouldIgnoreClientSecretPayload()
    {
        var client = CreateClient();
        var existingSecretResult = ClientSecret.Create(
            SecretHasher.HashSecret("existing-secret"),
            "Existing secret",
            DateTime.UtcNow.AddDays(30),
            out var existingSecret);

        existingSecretResult.IsSuccess.Should().BeTrue();
        client.AddSecret(existingSecret!).IsSuccess.Should().BeTrue();

        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetClientAggregateAsync(8, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        clientRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var apiResourceRepository = new Mock<IApiResourceRepository>();
        apiResourceRepository
            .Setup(x => x.GetEnabledApiResourcesAsync(
                1,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApiResourceValidationItem>());

        var tenantRepository = new Mock<ITenantRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var validator = new ClientCommandValidator(
            clientRepository.Object,
            apiResourceRepository.Object,
            tenantRepository.Object,
            roleRepository.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.TenantId).Returns(1);

        var logger = new Mock<IAppLogger<ClientCommandUseCase>>();
        var sut = new ClientCommandUseCase(
            clientRepository.Object,
            currentUserService.Object,
            validator,
            logger.Object);

        var request = new CreateUpdateClient
        {
            Id = 8,
            ClientName = "Updated Client",
            Description = "Updated description",
            IconUrl = "https://cdn.example.com/icon.svg",
            AppType = ClientTypes.WebApp,
            AccessTokenType = TokenTypes.JWT,
            RedirectUri = "https://app.example.com/callback",
            LogoutRedirectUri = "https://app.example.com/logout",
            IsActive = true,
            ClientSecretExpiry = 30,
            AccessTokenLifetime = 60,
            AuthorizationCodeLifetime = 5,
            RefreshTokenExpiration = 30,
            RefreshTokenDeliveryMode = RefreshTokenDeliveryMode.Cookie,
            CibaEnabled = true,
            BackchannelTokenDeliveryMode = CibaTokenDeliveryModes.Poll,
            CibaDefaultExpirySeconds = 300,
            CibaMinIntervalSeconds = 5,
            RequireCibaUserCode = false,
            AllowCibaLoginHint = true,
            AllowCibaLoginHintToken = false,
            AllowCibaIdTokenHint = false,
            GrantTypes = new List<GrantTypes> { GrantTypes.authorization_code, GrantTypes.ciba },
            Scopes = new List<string> { "openid", "profile" },
            ApiResources = new List<string>(),
            ClientSecret = "new-secret-from-request",
            ClientSecretDescription = "Should be ignored on update",
            AuthPolicy = new ClientAuthPolicyDetail
            {
                AutoCreateUsers = false,
                ShowExternalProviders = false
            },
            ExternalProviders = new List<int>()
        };

        var result = await sut.UpdateClient(8, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        client.ClientSecrets.Should().HaveCount(1);
        client.ClientSecrets.Single().SecretHash.Should().Be(SecretHasher.HashSecret("existing-secret"));
        clientRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Client CreateClient()
    {
        var createResult = Client.Create(
            tenantId: 1,
            clientId: "client-1",
            clientName: "Test Client",
            description: null,
            iconUrl: null,
            appType: ClientTypes.WebApp,
            tokenType: TokenTypes.JWT,
            redirectUri: "https://app.example.com/callback",
            logoutRedirectUri: "https://app.example.com/logout",
            isActive: true,
            clientSecretExpiry: 30,
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 30,
            refreshTokenDeliveryMode: RefreshTokenDeliveryMode.Response,
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
        return client!;
    }
}
