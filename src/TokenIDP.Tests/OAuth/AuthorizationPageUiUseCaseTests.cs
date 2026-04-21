using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.AggregateRoots.Tenants;

namespace TokenIDP.Tests.OAuth;

public sealed class AuthorizationPageUiUseCaseTests
{
    [Fact]
    public async Task BuildAsync_ShouldApplyTenantBranding()
    {
        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository
            .Setup(x => x.GetSummaryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSummary
            {
                Id = 7,
                TenantName = "Acme",
                TenantDisplayName = "Acme Identity",
                TenantKey = "acme"
            });
        tenantRepository
            .Setup(x => x.GetTenantUISettings(7))
            .ReturnsAsync(TenantUISetting.Create("Light", "https://cdn.test/logo.png", "#123456", "en", "Welcome to Acme"));

        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetClientAuthPolicy(9))
            .ReturnsAsync((ClientAuthPolicy?)null);
        clientRepository
            .Setup(x => x.GetExternalProviders(9))
            .ReturnsAsync(Array.Empty<ClientExternalProviderSnapshot>());

        var sut = new AuthorizationPageUiUseCase(tenantRepository.Object, clientRepository.Object);

        var ui = await sut.BuildAsync(new HashSet<string> { "openid" }, 7, 9, CancellationToken.None);

        ui.ProductName.Should().Be("Acme Identity");
        ui.LogoUrl.Should().Be("https://cdn.test/logo.png");
        ui.AccentColor.Should().Be("#123456");
        ui.LoginText.Should().Be("Welcome to Acme");
    }
}
