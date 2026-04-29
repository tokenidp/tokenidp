using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Infrastructure.Outbox.Abstractions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Tests.OAuth;

public sealed class ClientRepositoryTests
{
    [Fact]
    public async Task GetActiveByClientId_ShouldResolveSystemClient_ForScopedOperationalTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var seedContext = CreateDbContext(
            databaseName,
            CreateCurrentUserService(userId: 1, tenantId: 0).Object,
            new TenantContextAccessor()))
        {
            await SeedTenantAsync(seedContext, tenantId: 1, tenantKey: "system", isSystemTenant: true);
            await SeedTenantAsync(seedContext, tenantId: 2, tenantKey: "smartdev", isSystemTenant: false);
            await SeedClientAsync(seedContext, tenantId: 1, clientId: "idp-admin");
            await seedContext.SaveChangesAsync();
        }

        var tenantContextAccessor = new TenantContextAccessor();
        var currentUserService = CreateCurrentUserService(userId: 99, tenantId: 2);

        await using var queryContext = CreateDbContext(databaseName, currentUserService.Object, tenantContextAccessor);
        var cache = new TokenIDP.Infrastructure.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IAppLogger<TokenIDP.Infrastructure.MemoryCache>>());
        var repository = new ClientRepository(
            queryContext,
            Mock.Of<IAppLogger<ClientRepository>>(),
            cache,
            currentUserService.Object,
            tenantContextAccessor);

        var client = await repository.GetActiveByClientId("idp-admin");

        client.ClientId.Should().Be("idp-admin");
        client.TenantId.Should().Be(1);
        client.IsSystemTenant.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientShortInfo_ShouldResolveSystemClient_WhenTenantContextTargetsOperationalTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var seedContext = CreateDbContext(
            databaseName,
            CreateCurrentUserService(userId: 1, tenantId: 0).Object,
            new TenantContextAccessor()))
        {
            await SeedTenantAsync(seedContext, tenantId: 1, tenantKey: "system", isSystemTenant: true);
            await SeedTenantAsync(seedContext, tenantId: 2, tenantKey: "smartdev", isSystemTenant: false);
            await SeedClientAsync(seedContext, tenantId: 1, clientId: "idp-admin");
            await seedContext.SaveChangesAsync();
        }

        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TenantContext(2, "smartdev", false));
        var currentUserService = CreateCurrentUserService(userId: 99, tenantId: 0);

        await using var queryContext = CreateDbContext(databaseName, currentUserService.Object, tenantContextAccessor);
        var cache = new TokenIDP.Infrastructure.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IAppLogger<TokenIDP.Infrastructure.MemoryCache>>());
        var repository = new ClientRepository(
            queryContext,
            Mock.Of<IAppLogger<ClientRepository>>(),
            cache,
            currentUserService.Object,
            tenantContextAccessor);

        var client = await repository.GetClientShortInfo("idp-admin");

        client.ClientName.Should().Be("Admin Portal");
        client.TenantId.Should().Be(1);
        client.IsSystemTenant.Should().BeTrue();
    }

    private static Mock<ICurrentUserService> CreateCurrentUserService(int userId, int tenantId)
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(userId);
        currentUserService.SetupGet(x => x.TenantId).Returns(tenantId);
        return currentUserService;
    }

    private static ApplicationDbContext CreateDbContext(
        string databaseName,
        ICurrentUserService currentUserService,
        ITenantContextAccessor tenantContextAccessor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(
            options,
            currentUserService,
            tenantContextAccessor,
            Mock.Of<IAppLogger<ApplicationDbContext>>(),
            Mock.Of<IOutboxMapperResolver>(),
            Mock.Of<IOutboxConsumerRouter>());
    }

    private static Task SeedTenantAsync(
        ApplicationDbContext dbContext,
        int tenantId,
        string tenantKey,
        bool isSystemTenant)
    {
        var createResult = Tenant.Create(
            tenantKey,
            tenantKey,
            $"{tenantKey}@example.com",
            true,
            TenantAuthSetting.Create(0),
            TenantUISetting.Create("Light", null, "#000", "en", null),
            isSystemTenant,
            out var tenant);

        createResult.IsSuccess.Should().BeTrue();
        SetProperty(tenant!, nameof(Tenant.Id), tenantId);
        tenant!.GenerateTenantCode(tenantId);
        dbContext.Tenants.Add(tenant);
        return Task.CompletedTask;
    }

    private static Task SeedClientAsync(
        ApplicationDbContext dbContext,
        int tenantId,
        string clientId)
    {
        var createResult = Client.Create(
            tenantId: tenantId,
            clientId: clientId,
            clientName: "Admin Portal",
            description: null,
            iconUrl: null,
            appType: ClientTypes.WebApp,
            tokenType: TokenTypes.JWT,
            redirectUri: "https://portal.example/auth/callback",
            logoutRedirectUri: "https://portal.example/login",
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
        dbContext.Clients.Add(client!);
        return Task.CompletedTask;
    }

    private static void SetProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var property = typeof(TTarget).GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        property.Should().NotBeNull();
        property!.SetValue(target, value);
    }
}
