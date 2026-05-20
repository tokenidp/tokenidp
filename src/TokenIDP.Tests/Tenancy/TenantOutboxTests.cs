using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Domain.DomainEvents.Tenants;
using TokenIDP.Domain.ReadModels.Enums;
using TokenIDP.Infrastructure.Persistence;
using TokenIDP.Workers.Mappers;
using TokenIDP.Workers.Projectors;

namespace TokenIDP.Tests.Tenancy;

public sealed class TenantOutboxTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistOutboxEvent_ForTenantBrandingChanges()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using var dbContext = CreateDbContext(databaseName);

        var createResult = Tenant.Create(
            tenantName: "Acme",
            tenantKey: "acme",
            email: "admin@acme.test",
            isActive: true,
            authSetting: TenantAuthSetting.Create(0),
            tenantUISetting: TenantUISetting.Create("Light", null, "#112233", "en", null),
            isSystemTenant: false,
            out var tenant);

        createResult.IsSuccess.Should().BeTrue();
        tenant.Should().NotBeNull();
        tenant!.GenerateTenantCode(1);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var updateResult = tenant!.UpdateBranding("Dark", "logo.svg", "#445566", "en", "Welcome");

        updateResult.IsSuccess.Should().BeTrue();

        await dbContext.SaveChangesAsync();

        var outboxEvent = await dbContext.OutboxEvents
            .Include(x => x.OutboxEventConsumers)
            .SingleAsync();

        outboxEvent.EventType.Should().Be("TenantUpdated");
        outboxEvent.AggregateType.Should().Be("Tenant");
        outboxEvent.TenantId.Should().Be(tenant.Id);
        outboxEvent.OutboxEventConsumers.Should().ContainSingle(x => x.ConsumerName == "Activity");
    }

    [Fact]
    public async Task ActivityProjector_ShouldCreateTenantManagementActivity_ForTenantBrandingChange()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using var dbContext = CreateDbContext(databaseName);
        var projector = new ActivityProjector(dbContext, Mock.Of<IAppLogger<ActivityProjector>>());

        var outboxEvent = OutboxEvent.Create(
            tenantId: 7,
            eventType: "TenantUpdated",
            aggregateId: "7",
            aggregateType: "Tenant",
            payload: new TenantBrandingChangedEvent(7, "acme"),
            partitionKey: "tenant:7:tenant");

        SetProperty(outboxEvent, nameof(OutboxEvent.Id), 123L);

        await projector.ProjectAsync(outboxEvent, CancellationToken.None);

        var activity = await dbContext.Activities.SingleAsync();
        activity.Category.Should().Be(ActivityCategory.TenantManagement);
        activity.EventType.Should().Be(ActivityEventType.TenantUpdated);
        activity.TargetId.Should().Be("7");
        activity.TargetDescription.Should().Be("acme");
        activity.Description.Should().Be("Tenant 'acme' branding updated.");
    }

    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(1);
        currentUserService.SetupGet(x => x.TenantId).Returns(0);

        var mappers = new IOutboxMapper[]
        {
            new TokenOutboxMapper(),
            new UserOutboxMapper(),
            new TenantOutboxMapper()
        };

        return new ApplicationDbContext(
            options,
            currentUserService.Object,
            new TenantContextAccessor(),
            Mock.Of<IAppLogger<ApplicationDbContext>>(),
            new OutboxMapperResolver(mappers),
            new OutboxConsumerRouter());
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
