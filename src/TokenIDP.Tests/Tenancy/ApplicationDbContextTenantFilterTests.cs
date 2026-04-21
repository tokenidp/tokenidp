using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Domain.AggregateRoots.Users;
using TokenIDP.Infrastructure.Outbox.Abstractions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Tests.Tenancy;

public sealed class ApplicationDbContextTenantFilterTests
{
    [Fact]
    public async Task QueryFilter_ShouldScopeTenantEntities_ToCurrentTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var seedContext = CreateDbContext(databaseName, new TenantContextAccessor(), userId: 1))
        {
            await SeedTenantAsync(seedContext, 1, "system", true);
            await SeedTenantAsync(seedContext, 2, "acme", false);
            await SeedUserAsync(seedContext, 1, 11, "system.user", "system@example.com");
            await SeedUserAsync(seedContext, 2, 22, "acme.user", "acme@example.com");
            await seedContext.SaveChangesAsync();
        }

        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TenantContext(2, "acme", false));

        await using var queryContext = CreateDbContext(databaseName, tenantContextAccessor, userId: 99);
        var users = await queryContext.Users.AsNoTracking().ToListAsync();

        users.Should().ContainSingle();
        users[0].TenantId.Should().Be(2);
    }

    private static ApplicationDbContext CreateDbContext(string databaseName, ITenantContextAccessor tenantContextAccessor, int userId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(userId);
        currentUserService.SetupGet(x => x.TenantId).Returns(tenantContextAccessor.HasTenant ? tenantContextAccessor.TenantId : 0);

        return new ApplicationDbContext(
            options,
            currentUserService.Object,
            tenantContextAccessor,
            Mock.Of<IAppLogger<ApplicationDbContext>>(),
            Mock.Of<IOutboxMapperResolver>(),
            Mock.Of<IOutboxConsumerRouter>());
    }

    private static async Task SeedTenantAsync(ApplicationDbContext dbContext, int tenantId, string tenantKey, bool isSystemTenant)
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
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(ApplicationDbContext dbContext, int tenantId, int userId, string userName, string email)
    {
        var createResult = User.Create(
            tenantId,
            "Test",
            "User",
            userName,
            email,
            "0000000000",
            1,
            Array.Empty<int>(),
            out var user);

        createResult.IsSuccess.Should().BeTrue();
        SetProperty(user!, nameof(User.Id), userId);
        user!.GenerateUserCode(userId);
        user.SetPasswordHash("hashed");
        dbContext.Users.Add(user);
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
