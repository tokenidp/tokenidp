using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.Security;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Tokens;

namespace TokenIDP.Tests.OAuth;

public class CibaTokenRedemptionUseCaseTests
{
    [Fact]
    public async Task RedeemAsync_ReturnsAuthorizationPending_WhenRequestNotApproved()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest();
        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out _, out _);

        var action = () => useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("authorization_pending");
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RedeemAsync_ReturnsSlowDown_WhenClientPollsTooQuickly()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest(intervalSeconds: 2);
        Assert.Throws<DomainException>(() => cibaRequest.RegisterPoll());

        var originalInterval = cibaRequest.IntervalSeconds;
        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out _, out _);

        var action = () => useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("slow_down");
        cibaRequest.IntervalSeconds.Should().Be(originalInterval + 5);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RedeemAsync_ReturnsExpiredToken_WhenRequestExpired()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest();
        CibaTestData.SetProperty(cibaRequest, nameof(BackchannelAuthenticationRequest.ExpiresAtUtc), DateTime.UtcNow.AddSeconds(-1));

        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out _, out _);

        var action = () => useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("expired_token");
        cibaRequest.Status.Should().Be(CibaRequestStatus.Expired);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RedeemAsync_ReturnsAccessDenied_WhenRequestWasDenied()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest();
        cibaRequest.Deny("rejected");

        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out _, out _);

        var action = () => useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("access_denied");
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RedeemAsync_ReturnsInvalidGrant_WhenAuthReqIdBelongsToAnotherClient()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest(clientId: "other-client");
        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out _, out _);

        var action = () => useCase.RedeemAsync(CreateTokenRequest(clientId: "ciba-client"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("invalid_grant");
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(It.IsAny<BackchannelAuthenticationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RedeemAsync_IssuesTokens_WhenRequestApproved()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest();
        cibaRequest.Approve();

        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out var tokenRepository, out _);

        var response = await useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.IDToken.Should().NotBeNullOrWhiteSpace();
        cibaRequest.Status.Should().Be(CibaRequestStatus.TokenIssued);
        cibaRequest.ConsumedAtUtc.Should().NotBeNull();

        tokenRepository.Verify(x => x.CreateToken(It.IsAny<Token>()), Times.Once);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task RedeemAsync_PreventsReplay_AfterSuccessfulRedemption()
    {
        var cibaRequest = CibaTestData.CreatePendingRequest();
        cibaRequest.Approve();

        var useCase = CreateSut(cibaRequest, out var authorizationRepository, out var tokenRepository, out _);

        var first = await useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        CibaTestData.SetProperty(
            cibaRequest,
            nameof(BackchannelAuthenticationRequest.LastPolledAtUtc),
            DateTime.UtcNow.AddMinutes(-1));

        var action = () => useCase.RedeemAsync(CreateTokenRequest(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TokenRequestValidationException>(action);
        exception.Error.Should().Be("invalid_grant");
        tokenRepository.Verify(x => x.CreateToken(It.IsAny<Token>()), Times.Once);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(cibaRequest, It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    private static CibaTokenRedemptionUseCase CreateSut(
        BackchannelAuthenticationRequest cibaRequest,
        out Mock<IAuthorizationRepository> authorizationRepository,
        out Mock<ITokenRepository> tokenRepository,
        out Mock<IClientRepository> clientRepository)
    {
        var client = CibaTestData.CreateClientSnapshot();
        var userShortInfo = CibaTestData.CreateUserShortInfo();

        authorizationRepository = new Mock<IAuthorizationRepository>();
        tokenRepository = new Mock<ITokenRepository>();
        clientRepository = new Mock<IClientRepository>();
        var userRepository = new Mock<IUserRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var tenantRepository = new Mock<ITenantRepository>();

        authorizationRepository
            .Setup(x => x.GetBackchannelAuthenticationRequestByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cibaRequest);
        authorizationRepository
            .Setup(x => x.UpdateBackchannelAuthenticationRequest(It.IsAny<BackchannelAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        clientRepository
            .Setup(x => x.GetActiveByClientId(client.ClientId))
            .ReturnsAsync(client);

        userRepository
            .Setup(x => x.GetUserShortInfo(cibaRequest.UserId!.Value))
            .ReturnsAsync(userShortInfo);

        roleRepository
            .Setup(x => x.GetUserRoles(cibaRequest.UserId!.Value))
            .ReturnsAsync(new[] { "admin" });
        tenantRepository
            .Setup(x => x.GetSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int tenantId, CancellationToken _) => new TenantSummary
            {
                Id = tenantId,
                TenantKey = tenantId == 1 ? "system" : $"tenant-{tenantId}",
                TenantName = $"Tenant {tenantId}"
            });

        tokenRepository
            .Setup(x => x.CreateToken(It.IsAny<Token>()))
            .ReturnsAsync(1);
        tokenRepository
            .Setup(x => x.RemoveOldRefreshTokens(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        var tokenContextUseCase = new TokenContextUseCase(
            roleRepository.Object,
            clientRepository.Object,
            tenantRepository.Object,
            Mock.Of<IAppLogger<TokenContextUseCase>>(),
            userRepository.Object);

        var currentUserService = new TestCurrentUserService
        {
            UserId = cibaRequest.UserId!.Value,
            TenantId = cibaRequest.TenantId
        };

        var tokenIssuerUseCase = new TokenIssuerUseCase(
            new JwtTokenGenerator(
                Options.Create(CibaTestData.CreateTokenOptions()),
                currentUserService),
            Mock.Of<IAppLogger<TokenIssuerUseCase>>(),
            tokenRepository.Object,
            currentUserService,
            new TokenSecretGenerator());

        return new CibaTokenRedemptionUseCase(
            authorizationRepository.Object,
            clientRepository.Object,
            tokenContextUseCase,
            tokenIssuerUseCase);
    }

    private static TokenRequest CreateTokenRequest(
        string clientId = "ciba-client",
        string rawAuthReqId = "raw-auth-req-id")
    {
        return new TokenRequest
        {
            GrantType = TokenGrantTypeNames.Ciba,
            ClientId = clientId,
            ClientSecret = "secret",
            AuthReqId = rawAuthReqId
        };
    }
}
