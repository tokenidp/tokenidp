using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain.AggregateRoots.Authorization;

namespace TokenIDP.Tests.OAuth;

public class CibaBackchannelAuthenticationUseCaseTests
{
    [Fact]
    public async Task CreateAsync_CreatesPendingRequest_WhenLoginHintIsValid()
    {
        var client = CibaTestData.CreateClientSnapshot();
        var user = CibaTestData.CreateActiveUser();
        var authorizationRepository = new Mock<IAuthorizationRepository>();
        var clientRepository = new Mock<IClientRepository>();
        var userRepository = new Mock<IUserRepository>();

        BackchannelAuthenticationRequest? savedRequest = null;

        authorizationRepository
            .Setup(x => x.CreateBackchannelAuthenticationRequest(It.IsAny<BackchannelAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<BackchannelAuthenticationRequest, CancellationToken>((request, _) => savedRequest = request)
            .ReturnsAsync(1);

        clientRepository
            .Setup(x => x.GetActiveByClientId(client.ClientId))
            .ReturnsAsync(client);

        userRepository
            .Setup(x => x.FindByLoginHintAsync(user.TenantId, user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var resolver = new CibaUserResolver(
            userRepository.Object,
            new TestHostEnvironment(),
            Options.Create(CibaTestData.CreateTokenOptions()));

        var sut = new CibaBackchannelAuthenticationUseCase(
            authorizationRepository.Object,
            clientRepository.Object,
            resolver,
            Mock.Of<IAppLogger<CibaBackchannelAuthenticationUseCase>>());

        var request = new CibaBackchannelAuthenticationRequest
        {
            Scope = "openid profile",
            LoginHint = user.Email,
            BindingMessage = "12345"
        };
        request.SetClientAuthentication(client.ClientId, "secret", "client_secret_basic");
        request.SetTenantId(user.TenantId);

        var response = await sut.CreateAsync(request, CancellationToken.None);

        response.AuthReqId.Should().NotBeNullOrWhiteSpace();
        response.ExpiresIn.Should().Be(client.CibaDefaultExpirySeconds);
        response.Interval.Should().Be(client.CibaMinIntervalSeconds);

        savedRequest.Should().NotBeNull();
        savedRequest!.TenantId.Should().Be(user.TenantId);
        savedRequest.ClientId.Should().Be(client.ClientId);
        savedRequest.UserId.Should().Be(user.Id);
        savedRequest.RequestedScopes.Should().Be("openid profile");
        savedRequest.Status.Should().Be(CibaRequestStatus.AwaitingAuthorization);
        savedRequest.BindingMessage.Should().Be("12345");
        savedRequest.AuthReqIdHash.Should().NotBeNullOrWhiteSpace();
        savedRequest.AuthReqIdHash.Should().NotBe(response.AuthReqId);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenMoreThanOneHintIsProvided()
    {
        var client = CibaTestData.CreateClientSnapshot(allowLoginHint: true, allowLoginHintToken: true);
        var clientRepository = new Mock<IClientRepository>();
        var userRepository = new Mock<IUserRepository>();

        clientRepository
            .Setup(x => x.GetActiveByClientId(client.ClientId))
            .ReturnsAsync(client);

        var resolver = new CibaUserResolver(
            userRepository.Object,
            new TestHostEnvironment(),
            Options.Create(CibaTestData.CreateTokenOptions()));

        var sut = new CibaBackchannelAuthenticationUseCase(
            Mock.Of<IAuthorizationRepository>(),
            clientRepository.Object,
            resolver,
            Mock.Of<IAppLogger<CibaBackchannelAuthenticationUseCase>>());

        var request = new CibaBackchannelAuthenticationRequest
        {
            Scope = "openid profile",
            LoginHint = "user@example.com",
            LoginHintToken = "{\"sub\":\"101\"}"
        };
        request.SetClientAuthentication(client.ClientId, "secret", "client_secret_basic");
        request.SetTenantId(client.TenantId);

        var action = () => sut.CreateAsync(request, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BackchannelAuthenticationValidationException>(action);
        exception.Error.Should().Be("invalid_request");
        exception.Message.Should().Contain("Exactly one");
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNoHintIsProvided()
    {
        var client = CibaTestData.CreateClientSnapshot();
        var clientRepository = new Mock<IClientRepository>();
        var userRepository = new Mock<IUserRepository>();

        clientRepository
            .Setup(x => x.GetActiveByClientId(client.ClientId))
            .ReturnsAsync(client);

        var resolver = new CibaUserResolver(
            userRepository.Object,
            new TestHostEnvironment(),
            Options.Create(CibaTestData.CreateTokenOptions()));

        var sut = new CibaBackchannelAuthenticationUseCase(
            Mock.Of<IAuthorizationRepository>(),
            clientRepository.Object,
            resolver,
            Mock.Of<IAppLogger<CibaBackchannelAuthenticationUseCase>>());

        var request = new CibaBackchannelAuthenticationRequest
        {
            Scope = "openid profile"
        };
        request.SetClientAuthentication(client.ClientId, "secret", "client_secret_basic");
        request.SetTenantId(client.TenantId);

        var action = () => sut.CreateAsync(request, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BackchannelAuthenticationValidationException>(action);
        exception.Error.Should().Be("invalid_request");
        exception.Message.Should().Contain("Exactly one");
    }
}
