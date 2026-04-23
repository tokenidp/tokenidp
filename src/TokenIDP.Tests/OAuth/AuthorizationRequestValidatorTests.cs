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
}
