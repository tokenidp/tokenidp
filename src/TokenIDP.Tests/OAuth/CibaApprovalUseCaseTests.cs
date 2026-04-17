using Moq;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Exceptions;
using TokenIDP.Core.OAuth.UseCases;

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
            });

        var action = () => sut.ApproveAsync(request.Id, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
        authorizationRepository.Verify(
            x => x.UpdateBackchannelAuthenticationRequest(It.IsAny<Domain.AggregateRoots.Authorization.BackchannelAuthenticationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
