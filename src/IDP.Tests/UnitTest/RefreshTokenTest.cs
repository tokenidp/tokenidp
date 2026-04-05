using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IDP.Tests.UnitTest;

public class RefreshTokenControllerTests
{
    private readonly Mock<RefreshTokenService> _mockRefreshTokenService;
    private readonly Mock<IAppLogger<RefreshTokenController>> _mockLogger;
    private readonly RefreshTokenController _controller;

    public RefreshTokenControllerTests()
    {
        _mockRefreshTokenService = new Mock<RefreshTokenService>();
        _mockLogger = new Mock<IAppLogger<RefreshTokenController>>();
        _controller = new RefreshTokenController(_mockRefreshTokenService.Object, _mockLogger.Object);

        // Setup default HttpContext with mock IP
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection =
                {
                    RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1")
                }
            }
        };
    }

    [Fact]
    public async Task GetRefreshToken_ValidRequest_ReturnsTokenResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token",
            ClientId = "client1"
        };

        var expectedResponse = TokenResponse.Create(1, "", DateTime.Now);

        _mockRefreshTokenService
            .Setup(x => x.GenerateRefreshToken(
                request.RefreshToken,
                request.ClientId,
                "192.168.1.1"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetRefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<TokenResponse>>(okResult.Value);
        Assert.Equal(expectedResponse, resultValue.Value);

        _mockLogger.Verify(x => x.LogInfo(
            "GetRefreshToken called from IP: {IP}", "192.168.1.1"), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "Refresh token generated for ClientId: {ClientId}", request.ClientId), Times.Once);
    }

    [Fact]
    public async Task GetRefreshToken_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetRefreshToken(null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetRefreshToken_ServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-token",
            ClientId = "client1"
        };
        var exception = new Exception("Invalid refresh token");

        _mockRefreshTokenService
            .Setup(x => x.GenerateRefreshToken(
                request.RefreshToken,
                request.ClientId,
                "192.168.1.1"))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRefreshToken(request));
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task GetRefreshToken_NoIpAddress_UsesFallbackIp()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-token",
            ClientId = "client1"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No IP set
        };

        // Act
        await _controller.GetRefreshToken(request);

        // Assert
        _mockRefreshTokenService.Verify(x => x.GenerateRefreshToken(
            request.RefreshToken,
            request.ClientId,
            "0.0.0.0"), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RevokeTokenRequest
        {
            RefreshToken = "token-to-revoke",
            ReasonRevoked = "User logout"
        };

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<object>>(okResult.Value);
        Assert.Equal("Refresh token revoked.", ((dynamic)resultValue.Value).message);

        _mockLogger.Verify(x => x.LogInfo(
            "RevokeToken called from IP: {IP}, Reason: {Reason}",
            "192.168.1.1", request.ReasonRevoked), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "Refresh token revoked for IP: {IP}", "192.168.1.1"), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RevokeToken(null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_ServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        var request = new RevokeTokenRequest
        {
            RefreshToken = "invalid-token",
            ReasonRevoked = "Suspicious activity"
        };
        var exception = new Exception("Token not found");

        _mockRefreshTokenService
            .Setup(x => x.RevokeRefreshToken(
                request.RefreshToken,
                "192.168.1.1",
                request.ReasonRevoked))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.RevokeToken(request));
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_VerifyServiceCalledWithCorrectParameters()
    {
        // Arrange
        var request = new RevokeTokenRequest
        {
            RefreshToken = "token123",
            ReasonRevoked = "Expired"
        };

        // Act
        await _controller.RevokeToken(request);

        // Assert
        _mockRefreshTokenService.Verify(x => x.RevokeRefreshToken(
            request.RefreshToken,
            "192.168.1.1",
            request.ReasonRevoked), Times.Once);
    }
}