using FluentAssertions;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Exceptions;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.Base;

namespace TokenIDP.Tests.OAuth;

public class CibaApprovalUseCaseTests
{
    [Fact]
    public async Task ApproveAsync_EnforcesTenantIsolation()
    {
        var request = CibaTestData.CreatePendingRequest(tenantId: 1, userId: 101);
        var authorizationRepository = new Mock<IAuthorizationRepository>();

        authorizationRepository
            .Setup(x => x.GetBackchannelAuthenticationRequestByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var sut = new CibaApprovalUseCase(
            authorizationRepository.Object,
            Mock.Of<IClientRepository>(),
            new TestCurrentUserService
            {
                UserId = 101,
                TenantId = 2
            },
            Mock.Of<IApplicationEventDispatcher>());

        var action = () => sut.ApproveAsync(request.Id, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(It.IsAny<Domain.AggregateRoots.Authorization.BackchannelAuthenticationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetApprovalChallengeAsync_DoesNotApproveRequest()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        var challenge = await sut.GetApprovalChallengeAsync(
            request.PublicRequestId,
            token,
            recordPageOpened: true,
            CancellationToken.None);

        challenge.PublicRequestId.Should().Be(request.PublicRequestId);
        request.Status.Should().Be(CibaRequestStatus.AwaitingAuthorization);
    }

    [Fact]
    public async Task ApproveWithTokenAsync_RejectsInvalidApprovalToken()
    {
        var request = CibaTestData.CreatePendingRequest(approvalToken: "approval-token");
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        var action = () => sut.ApproveWithTokenAsync(
            request.PublicRequestId,
            "wrong-token",
            ipAddress: null,
            userAgent: null,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(action);
        exception.Message.Should().Be("invalid_token");
        request.Status.Should().Be(CibaRequestStatus.AwaitingAuthorization);
    }

    [Fact]
    public async Task ApproveWithTokenAsync_RejectsExpiredRequest()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        CibaTestData.SetProperty(request, nameof(BackchannelAuthenticationRequest.ExpiresAtUtc), DateTime.UtcNow.AddSeconds(-1));
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        var action = () => sut.ApproveWithTokenAsync(
            request.PublicRequestId,
            token,
            ipAddress: null,
            userAgent: null,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(action);
        exception.Message.Should().Be("expired_token");
        request.Status.Should().Be(CibaRequestStatus.Expired);
    }

    [Fact]
    public async Task RejectWithTokenAsync_MarksRequestDenied()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        await sut.RejectWithTokenAsync(
            request.PublicRequestId,
            token,
            ipAddress: "127.0.0.1",
            userAgent: "test",
            CancellationToken.None);

        request.Status.Should().Be(CibaRequestStatus.Denied);
        request.DecisionByUserId.Should().Be(request.UserId);
        request.DeniedAtUtc.Should().NotBeNull();
        request.ApprovalDecisionAtUtc.Should().Be(request.DeniedAtUtc);
        request.ApprovalTokenConsumedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveWithTokenAsync_MarksRequestApproved()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        await sut.ApproveWithTokenAsync(
            request.PublicRequestId,
            token,
            ipAddress: "127.0.0.1",
            userAgent: "test",
            CancellationToken.None);

        request.Status.Should().Be(CibaRequestStatus.Approved);
        request.DecisionByUserId.Should().Be(request.UserId);
        request.ApprovedAtUtc.Should().NotBeNull();
        request.ApprovalDecisionAtUtc.Should().Be(request.ApprovedAtUtc);
        request.ApprovalTokenConsumedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveWithTokenAsync_RejectsConsumedApprovalToken()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        request.ConsumeApprovalToken();
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        var action = () => sut.ApproveWithTokenAsync(
            request.PublicRequestId,
            token,
            ipAddress: null,
            userAgent: null,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(action);
        exception.Message.Should().Be("invalid_token");
        request.Status.Should().Be(CibaRequestStatus.AwaitingAuthorization);
    }

    [Fact]
    public async Task GetApprovalChallengeAsync_RejectsExpiredApprovalToken()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        CibaTestData.SetProperty(request, nameof(BackchannelAuthenticationRequest.ApprovalTokenExpiresAtUtc), DateTime.UtcNow.AddSeconds(-1));
        var sut = CreateSut(request, userId: request.UserId!.Value, tenantId: request.TenantId);

        var action = () => sut.GetApprovalChallengeAsync(
            request.PublicRequestId,
            token,
            recordPageOpened: true,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(action);
        exception.Message.Should().Be("expired_token");
        request.Status.Should().Be(CibaRequestStatus.AwaitingAuthorization);
    }

    [Fact]
    public void SetApprovalChallenge_StoresTokenHashInsteadOfRawToken()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);

        request.ApprovalTokenHash.Should().NotBe(token);
        request.ApprovalTokenHash.Should().NotBeNullOrWhiteSpace();
        request.ApprovalTokenUserHintHash.Should().Be("hint-hash");
    }

    private static CibaApprovalUseCase CreateSut(
        BackchannelAuthenticationRequest request,
        int userId,
        int tenantId)
    {
        var authorizationRepository = new Mock<IAuthorizationRepository>();
        var clientRepository = new Mock<IClientRepository>();
        var eventDispatcher = new Mock<IApplicationEventDispatcher>();

        authorizationRepository
            .Setup(x => x.GetBackchannelAuthenticationRequestByPublicIdAsync(request.PublicRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        authorizationRepository
            .Setup(x => x.UpdateBackchannelAuthenticationRequest(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request.Id);

        clientRepository
            .Setup(x => x.GetClientShortInfo(request.ClientId))
            .ReturnsAsync(new ClientShortInfo(
                id: 1,
                tenantId: request.TenantId,
                allowForgotPassword: false,
                clientName: "CIBA Client",
                redirectUri: "https://client.example/callback",
                requiredPkce: false,
                scopes: new[] { "openid", "profile" },
                grantTypes: new[] { GrantTypes.ciba }));

        eventDispatcher
            .Setup(x => x.RaiseAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new CibaApprovalUseCase(
            authorizationRepository.Object,
            clientRepository.Object,
            new TestCurrentUserService
            {
                UserId = userId,
                TenantId = tenantId
            },
            eventDispatcher.Object);
    }
}
