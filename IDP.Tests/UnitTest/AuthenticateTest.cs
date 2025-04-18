using IDP.Service.Application;
using IDP.Service.Controllers;
using IDP.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services.Common.Interfaces;
using Services.Common.Model;

namespace IDP.Tests.UnitTest;


public class AuthenticateControllerTests
{
    private readonly Mock<IdentityService> _mockIdentityService;
    private readonly Mock<IAppLogger<AuthenticateController>> _mockLogger;
    private readonly AuthenticateController _controller;

    public AuthenticateControllerTests()
    {
        _mockIdentityService = new Mock<IdentityService>();
        _mockLogger = new Mock<IAppLogger<AuthenticateController>>();
        _controller = new AuthenticateController(_mockIdentityService.Object, _mockLogger.Object);
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
