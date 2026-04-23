using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Exceptions;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.OAuth;

public sealed class AuthorizationRequestValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ShouldTranslateMissingAuthorizeClient_ToOAuthError()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetClientShortInfo("missing-client"))
            .ThrowsAsync(new NotFoundException("Client not found."));

        var tenantContextAccessor = new TenantContextAccessor();
        var validator = new AuthorizationRequestValidator(
            clientRepository.Object,
            tenantContextAccessor);

        var request = new AuthorizationRequest
        {
            ClientId = "missing-client",
            RedirectUri = "https://portal.example/auth/callback",
            ResponseType = "code",
            Scopes = "openid profile"
        };

        var action = async () => await validator.ValidateAsync(request, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<AuthorizationRequestException>();
        exception.Which.Error.Should().Be("unauthorized_client");
        exception.Which.ErrorDescription.Should().Be("Invalid client_id.");
        exception.Which.AllowRedirect.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ShouldTranslateMissingDeviceClient_ToOAuthError()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetClientShortInfo("missing-device-client"))
            .ThrowsAsync(new NotFoundException("Client not found."));

        var tenantContextAccessor = new TenantContextAccessor();
        var validator = new AuthorizationRequestValidator(
            clientRepository.Object,
            tenantContextAccessor);

        var request = new DeviceAuthorizationRequest
        {
            ClientId = "missing-device-client",
            Scope = StandardScopes.OpenId
        };

        var action = async () => await validator.ValidateAsync(request, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<AuthorizationRequestException>();
        exception.Which.Error.Should().Be("unauthorized_client");
        exception.Which.ErrorDescription.Should().Be("Invalid client_id.");
        exception.Which.AllowRedirect.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ShouldAllowSystemTenantClient_ForOperationalTenantRequest()
    {
        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetClientShortInfo("idp-admin"))
            .ReturnsAsync(new ClientShortInfo(
                id: 10,
                tenantId: 1,
                isSystemTenant: true,
                allowForgotPassword: false,
                clientName: "Admin Portal",
                redirectUri: "https://portal.example/auth/callback",
                requiredPkce: true,
                scopes: new[] { StandardScopes.OpenId, StandardScopes.Profile },
                grantTypes: new[] { GrantTypes.authorization_code }));

        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TokenIDP.Core.Abstractions.TenantContext(
            TenantId: 22,
            TenantKey: "smartdev",
            IsSystemTenant: false));

        var validator = new AuthorizationRequestValidator(
            clientRepository.Object,
            tenantContextAccessor);

        var request = new AuthorizationRequest
        {
            ClientId = "idp-admin",
            RedirectUri = "https://portal.example/auth/callback",
            ResponseType = "code",
            Scopes = "openid profile"
        };

        var result = await validator.ValidateAsync(request, CancellationToken.None);

        result.TenantId.Should().Be(1);
        result.IsSystemTenant.Should().BeTrue();
    }
}
