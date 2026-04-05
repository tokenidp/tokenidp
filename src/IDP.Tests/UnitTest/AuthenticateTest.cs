using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IDP.Tests.UnitTest;


public class AuthenticateControllerTests
{
    private readonly Mock<IdentityService> _mockIdentityService;
    private readonly Mock<IAppLogger<AuthenticationController>> _mockLogger;
    private readonly AuthenticationController _controller;
    private readonly Mock<MfaService> _mfaService;

    public AuthenticateControllerTests()
    {
        _mockIdentityService = new Mock<IdentityService>();
        _mockLogger = new Mock<IAppLogger<AuthenticationController>>();
        _mfaService = new Mock<MfaService>();
        _controller = new AuthenticationController(_mockIdentityService.Object,
            _mockLogger.Object, _mfaService.Object);
    }

    [Fact]
    public async Task Authenticate_ValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = new AuthRequest { UserName = "testuser", Password = "validPass123" };
        var expectedResponse = AuthResponse.Success("234324");

        _mockIdentityService.Setup(x => x.Authenticate(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Authenticate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<AuthResponse>>(okResult.Value);
        Assert.Equal(expectedResponse, resultValue.Value);

        _mockLogger.Verify(x => x.LogInfo(
            "Authenticate called for user: {Username}", request.UserName), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "Authenticate completed for user: {Username}", request.UserName), Times.Once);
    }

    [Fact]
    public async Task Authenticate_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Authenticate(null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Authenticate_ServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        var request = new AuthRequest { UserName = "testuser", Password = "validPass123" };
        var exception = new Exception("Authentication failed");

        _mockIdentityService.Setup(x => x.Authenticate(request))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.Authenticate(request));
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), exception), Times.Once);
    }
}
