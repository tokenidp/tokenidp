using FluentAssertions;
using Moq;
using TokenIDP.Core.Admin.Tenants;
using TokenIDP.Core.Admin.Tenants.UseCases;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Tests.OAuth;

namespace TokenIDP.Tests.Admin.Tenants;

public sealed class TenantCommandUseCaseTests
{
    [Fact]
    public async Task SuspendTenant_ShouldInvalidateTenantLookupCache()
    {
        var tenant = CreateTenant(id: 7, tenantKey: "acme");

        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository
            .Setup(x => x.GetTenantAggregateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        tenantRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var cache = new Mock<ICache>();
        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TenantContext(1, "system", true));

        var sut = new TenantCommandUseCase(
            tenantRepository.Object,
            Mock.Of<IClientRepository>(),
            Mock.Of<ITenantBootstrapper>(),
            cache.Object,
            new TestCurrentUserService { TenantId = 1 },
            tenantContextAccessor,
            Mock.Of<IAppLogger<TenantCommandUseCase>>(),
            Mock.Of<ISecretProtector>());

        var result = await sut.SuspendTenant(7, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.RemoveAsync("urn:TNT:lookup:acme"), Times.Once);
    }

    private static Tenant CreateTenant(int id, string tenantKey)
    {
        var createResult = Tenant.Create(
            "Acme",
            tenantKey,
            "admin@acme.test",
            true,
            TenantAuthSetting.Create(0),
            TenantUISetting.Create("Light", null, "#000", "en", null),
            false,
            out var tenant);

        createResult.IsSuccess.Should().BeTrue();
        var property = typeof(Tenant).GetProperty(
            nameof(Tenant.Id),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        property!.SetValue(tenant, id);

        return tenant!;
    }
}
