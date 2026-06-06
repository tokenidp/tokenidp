using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Domain.Base;
using TokenIDP.Infrastructure.Bootstrap;
using TokenIDP.Infrastructure.Outbox.Abstractions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Tests.Tenancy;

public sealed class SystemBootstrapperTests
{
    [Fact]
    public async Task EnsureSystemTenantAsync_ShouldCreateSystemTenantWithDefaultUiSettings()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using var dbContext = CreateDbContext(databaseName);
        var bootstrapper = CreateBootstrapper();

        var tenant = await bootstrapper.EnsureSystemTenantAsync(dbContext, CancellationToken.None);

        var persisted = await dbContext.Tenants
            .Include(x => x.TenantUISetting)
            .SingleAsync(x => x.Id == tenant.Id);

        persisted.TenantUISetting.LogoUrl.Should().Be("/_content/TokenIDP.Server/images/TokenIDP.svg");
        persisted.TenantUISetting.LoginText.Should().Be("Sign in to TokenIDP");
        persisted.TenantUISetting.Theme.Should().Be("Light");
        persisted.TenantUISetting.PrimaryColor.Should().Be("default");
        persisted.TenantUISetting.DefaultLanguage.Should().Be("en");
    }

    [Fact]
    public async Task EnsureSystemTenantAsync_ShouldNormalizeLegacySystemTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using var dbContext = CreateDbContext(databaseName);

        var createResult = Tenant.Create(
            tenantName: "system",
            tenantKey: "legacy-system",
            email: "admin@system.local",
            isActive: false,
            authSetting: TenantAuthSetting.Create(0),
            tenantUISetting: TenantUISetting.Create("Light", "default", "default", "en", string.Empty),
            isSystemTenant: false,
            out var tenant);

        createResult.IsSuccess.Should().BeTrue();
        tenant.Should().NotBeNull();

        tenant!.GenerateTenantCode(42);
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var bootstrapper = CreateBootstrapper();

        var resolved = await bootstrapper.EnsureSystemTenantAsync(dbContext, CancellationToken.None);

        resolved.TenantKey.Should().Be("system");
        resolved.IsSystemTenant.Should().BeTrue();
        resolved.IsActive.Should().BeTrue();

        var persisted = await dbContext.Tenants.SingleAsync();
        persisted.TenantKey.Should().Be("system");
        persisted.IsSystemTenant.Should().BeTrue();
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSystemTenantAsync_ShouldNormalizeExistingSystemTenantUiSettings()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using var dbContext = CreateDbContext(databaseName);

        var createResult = Tenant.Create(
            tenantName: "system",
            tenantKey: "system",
            email: "admin@system.local",
            isActive: true,
            authSetting: TenantAuthSetting.Create(0),
            tenantUISetting: TenantUISetting.Create("Light", "default", "default", "en", string.Empty),
            isSystemTenant: true,
            out var tenant);

        createResult.IsSuccess.Should().BeTrue();
        tenant.Should().NotBeNull();

        tenant!.GenerateTenantCode(1);
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var bootstrapper = CreateBootstrapper();

        var resolved = await bootstrapper.EnsureSystemTenantAsync(dbContext, CancellationToken.None);

        var persisted = await dbContext.Tenants
            .Include(x => x.TenantUISetting)
            .SingleAsync(x => x.Id == resolved.Id);

        persisted.TenantUISetting.LogoUrl.Should().Be("/_content/TokenIDP.Server/images/TokenIDP.svg");
        persisted.TenantUISetting.LoginText.Should().Be("Sign in to TokenIDP");
    }

    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(1);
        currentUserService.SetupGet(x => x.TenantId).Returns(0);

        var outboxResolver = new Mock<IOutboxMapperResolver>();
        outboxResolver
            .Setup(x => x.Resolve(It.IsAny<IDomainEvent>()))
            .Returns<IDomainEvent>(evt => OutboxEvent.Create(
                tenantId: 0,
                eventType: evt.GetType().Name,
                aggregateId: "bootstrap",
                aggregateType: "Tenant",
                payload: new { evtType = evt.GetType().Name }));

        var consumerRouter = new Mock<IOutboxConsumerRouter>();
        consumerRouter
            .Setup(x => x.ResolveConsumers(It.IsAny<IDomainEvent>()))
            .Returns(Array.Empty<string>());

        return new ApplicationDbContext(
            options,
            currentUserService.Object,
            new TenantContextAccessor(),
            Mock.Of<IAppLogger<ApplicationDbContext>>(),
            outboxResolver.Object,
            consumerRouter.Object);
    }

    private static SystemBootstrapper CreateBootstrapper()
    {
        var configuration = new ConfigurationBuilder().Build();

        return new SystemBootstrapper(
            new TenantProvisioningService(),
            Mock.Of<IClientProvisioningService>(),
            Mock.Of<IUserProvisioningService>(),
            Mock.Of<IRoleProvisioningService>(),
            Mock.Of<IPermissionSeeder>(),
            Mock.Of<IConfigurationSeeder>(),
            configuration,
            Options.Create(new BootstrapOption()),
            Mock.Of<IAppLogger<SystemBootstrapper>>());
    }
}
