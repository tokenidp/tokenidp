using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Tenants;
using TokenIDP.Core.Admin.Tenants.UseCases;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Tests.OAuth;

namespace TokenIDP.Tests.Admin;

public sealed class AdminWorkflowGuardTests
{
    [Fact]
    public async Task CreateTenant_ShouldRejectNonSystemTenant()
    {
        var sut = CreateTenantUseCase(currentTenantId: 22, contextTenantId: 22, isSystemTenant: false);

        var result = await sut.CreateTenant(new CreateUpdateTenant
        {
            TenantName = "Blocked Tenant",
            TenantKey = "blocked",
            Email = "admin@blocked.test",
            IsActive = true
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Error.Should().Be("Only the system tenant can create tenants.");
    }

    [Fact]
    public async Task UpdateTenant_ShouldRejectCrossTenantOperationalAccess()
    {
        var sut = CreateTenantUseCase(currentTenantId: 22, contextTenantId: 22, isSystemTenant: false);

        var result = await sut.UpdateTenant(33, new CreateUpdateTenant
        {
            Id = 33,
            TenantName = "Other Tenant",
            TenantKey = "other",
            Email = "admin@other.test",
            IsActive = true
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Error.Should().Be("Cross-tenant access is not allowed.");
    }

    [Fact]
    public async Task ActivateTenant_ShouldRejectOperationalTenant()
    {
        var sut = CreateTenantUseCase(currentTenantId: 22, contextTenantId: 22, isSystemTenant: false);

        var result = await sut.ActivateTenant(22);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Error.Should().Be("Only the system tenant can activate tenants.");
    }

    [Theory]
    [InlineData(0, null, null, "client.rate_limit.permit.invalid")]
    [InlineData(10, null, -1, "client.rate_limit.queue.invalid")]
    public void ClientCreate_ShouldRejectInvalidRateLimitSettings(
        int? permitLimit,
        int? windowSeconds,
        int? queueLimit,
        string expectedCode)
    {
        var result = Client.Create(
            tenantId: 7,
            clientId: "rate-limited-client",
            clientName: "Rate Limited Client",
            description: null,
            iconUrl: null,
            appType: ClientTypes.WebApp,
            tokenType: TokenTypes.JWT,
            redirectUri: "https://app.example/callback",
            logoutRedirectUri: null,
            isActive: true,
            clientSecretExpiry: null,
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 30,
            refreshTokenDeliveryMode: RefreshTokenDeliveryMode.Response,
            permitLimit,
            timeWindow: windowSeconds.HasValue ? TimeSpan.FromSeconds(windowSeconds.Value) : TimeSpan.FromMinutes(1),
            queueLimit,
            enableITracking: false,
            cibaEnabled: false,
            backchannelTokenDeliveryMode: CibaTokenDeliveryModes.Poll,
            cibaDefaultExpirySeconds: 300,
            cibaMinIntervalSeconds: 5,
            requireCibaUserCode: false,
            allowCibaLoginHint: true,
            allowCibaLoginHintToken: false,
            allowCibaIdTokenHint: false,
            out _);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == expectedCode);
    }

    [Fact]
    public async Task ValidateForSaveAsync_ShouldRejectExternalProviderOutsideTenant()
    {
        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository
            .Setup(x => x.GetTenantExternalProviderIdsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 10 });

        var sut = CreateClientCommandValidator(tenantRepository: tenantRepository.Object);
        var command = CreateValidClientCommand(
            tenantId: 7,
            authPolicy: new ClientAuthPolicyDetail
            {
                AutoCreateUsers = false,
                ShowExternalProviders = true
            },
            externalProviders: new List<int> { 99 });

        var result = await sut.ValidateForSaveAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "client.external_providers.invalid");
    }

    [Fact]
    public async Task ValidateForSaveAsync_ShouldRejectDefaultRoleOutsideTenant()
    {
        var roleRepository = new Mock<IRoleRepository>();
        roleRepository
            .Setup(x => x.GetRoleAssignmentValidationAsync(7, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignmentValidation?)null);

        var sut = CreateClientCommandValidator(roleRepository: roleRepository.Object);
        var command = CreateValidClientCommand(tenantId: 7);
        command.AuthPolicy.AutoCreateUsers = true;
        command.AuthPolicy.DefaultRoleId = 99;

        var result = await sut.ValidateForSaveAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "client.auth_policy.default_role.invalid");
    }

    private static TenantCommandUseCase CreateTenantUseCase(
        int currentTenantId,
        int contextTenantId,
        bool isSystemTenant)
    {
        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TenantContext(
            contextTenantId,
            $"tenant-{contextTenantId}",
            isSystemTenant));

        return new TenantCommandUseCase(
            Mock.Of<ITenantRepository>(),
            Mock.Of<IClientRepository>(),
            Mock.Of<ITenantBootstrapper>(),
            Mock.Of<ICache>(),
            new TestCurrentUserService { TenantId = currentTenantId },
            tenantContextAccessor,
            Mock.Of<IAppLogger<TenantCommandUseCase>>(),
            Mock.Of<ISecretProtector>());
    }

    private static ClientCommandValidator CreateClientCommandValidator(
        ITenantRepository? tenantRepository = null,
        IRoleRepository? roleRepository = null)
    {
        var apiResourceRepository = new Mock<IApiResourceRepository>();
        apiResourceRepository
            .Setup(x => x.GetEnabledApiResourcesAsync(
                It.IsAny<int>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApiResourceValidationItem>());

        var defaultTenantRepository = new Mock<ITenantRepository>();
        defaultTenantRepository
            .Setup(x => x.GetTenantExternalProviderIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var defaultRoleRepository = new Mock<IRoleRepository>();

        return new ClientCommandValidator(
            Mock.Of<IClientRepository>(),
            apiResourceRepository.Object,
            tenantRepository ?? defaultTenantRepository.Object,
            roleRepository ?? defaultRoleRepository.Object);
    }

    private static NormalizedClientCommand CreateValidClientCommand(
        int tenantId,
        ClientAuthPolicyDetail? authPolicy = null,
        List<int>? externalProviders = null)
    {
        return NormalizedClientCommand.Create(new CreateUpdateClient
        {
            ClientName = "Tenant Client",
            RedirectUri = "https://app.example/callback",
            AppType = ClientTypes.WebApp,
            AccessTokenType = TokenTypes.JWT,
            IsActive = true,
            AccessTokenLifetime = 60,
            AuthorizationCodeLifetime = 5,
            RefreshTokenExpiration = 30,
            RefreshTokenDeliveryMode = RefreshTokenDeliveryMode.Response,
            PermitLimit = 10,
            TimeWindow = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            GrantTypes = new List<GrantTypes> { GrantTypes.authorization_code },
            Scopes = new List<string> { StandardScopes.OpenId },
            ApiResources = new List<string>(),
            AuthPolicy = authPolicy ?? new ClientAuthPolicyDetail { AutoCreateUsers = false },
            ExternalProviders = externalProviders ?? new List<int>()
        }, tenantId);
    }
}
