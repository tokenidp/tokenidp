using IDP.Service.Application;
using IDP.Service.Application.TokenService;
using IDP.Service.Controllers;
using IDP.Service.Infrastructure;
using IDP.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services.Common.Interfaces;
using Services.Common.Model;
using static IDP.Service.Domain.Client;

namespace IDP.Tests.UnitTest;

public class TokenControllerTests
{
    private readonly Mock<TokenServiceFactory> _mockTokenServiceFactory;
    private readonly Mock<ClientService> _mockClientService;
    private readonly Mock<IAppLogger<TokenController>> _mockLogger;
    private readonly TokenController _controller;

    public TokenControllerTests()
    {
        _mockTokenServiceFactory = new Mock<TokenServiceFactory>();
        _mockClientService = new Mock<ClientService>();
        _mockLogger = new Mock<IAppLogger<TokenController>>();

        _controller = new TokenController(
            _mockTokenServiceFactory.Object,
            _mockClientService.Object,
            _mockLogger.Object);

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
    public async Task GetAccessToken_ValidRequest_ReturnsTokenResponse()
    {
        // Arrange
        var request = new TokenRequest { ClientId = "client1" };
        var tokenType = TokenType.JWT;
        var expectedResponse = TokenResponse.Create(1, "", DateTime.Now);
        var mockTokenService = new Mock<ITokenService>();

        _mockClientService.Setup(x => x.GetClientTokenType(request.ClientId))
            .ReturnsAsync(tokenType);

        _mockTokenServiceFactory.Setup(x => x.GetService(tokenType))
            .Returns(mockTokenService.Object);

        mockTokenService.Setup(x => x.GenerateToken(request, "192.168.1.1"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAccessToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<TokenResponse>>(okResult.Value);
        Assert.Equal(expectedResponse, resultValue.Value);

        _mockLogger.Verify(x => x.LogInfo(
            "GetAccessToken called for ClientId: {ClientId} from IP: {IP}",
            request.ClientId, "192.168.1.1"), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "Token generated for ClientId: {ClientId} with TokenType: {TokenType}",
            request.ClientId, tokenType), Times.Once);
    }

    [Fact]
    public async Task GetAccessToken_InvalidClient_ReturnsBadRequest()
    {
        // Arrange
        var request = new TokenRequest { ClientId = "invalid-client" };

        _mockClientService.Setup(x => x.GetClientTokenType(request.ClientId))
            .ReturnsAsync((TokenType)999); // Undefined enum value

        // Act
        var result = await _controller.GetAccessToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var resultValue = Assert.IsType<Result<TokenResponse>>(badRequestResult.Value);
        Assert.False(resultValue.IsSuccess);
        Assert.False(resultValue.IsSuccess);

        _mockLogger.Verify(x => x.LogWarning(
            "TokenType not found for ClientId: {ClientId}", request.ClientId), Times.Once);
    }

    [Fact]
    public async Task GetAccessToken_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAccessToken(null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetAccessToken_ServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        var request = new TokenRequest { ClientId = "client1" };
        var exception = new Exception("Token generation failed");

        _mockClientService.Setup(x => x.GetClientTokenType(request.ClientId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAccessToken(request));
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task GetAccessToken_ValidRequest_CallsCorrectTokenService()
    {
        // Arrange
        var request = new TokenRequest { ClientId = "client1" };
        var tokenType = TokenType.ReferenceToken;
        var mockTokenService = new Mock<ITokenService>();

        _mockClientService.Setup(x => x.GetClientTokenType(request.ClientId))
            .ReturnsAsync(tokenType);

        _mockTokenServiceFactory.Setup(x => x.GetService(tokenType))
            .Returns(mockTokenService.Object);

        // Act
        await _controller.GetAccessToken(request);

        // Assert
        _mockTokenServiceFactory.Verify(x => x.GetService(tokenType), Times.Once);
        mockTokenService.Verify(x => x.GenerateToken(request, "192.168.1.1"), Times.Once);
    }

    [Fact]
    public async Task GetAccessToken_NoIpAddress_UsesFallbackIp()
    {
        // Arrange
        var request = new TokenRequest { ClientId = "client1" };
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; // No IP

        var mockTokenService = new Mock<ITokenService>();
        _mockClientService.Setup(x => x.GetClientTokenType(request.ClientId))
            .ReturnsAsync(TokenType.JWT);
        _mockTokenServiceFactory.Setup(x => x.GetService(It.IsAny<TokenType>()))
            .Returns(mockTokenService.Object);

        // Act
        await _controller.GetAccessToken(request);

        // Assert
        mockTokenService.Verify(x => x.GenerateToken(request, "0.0.0.0"), Times.Once);
    }
}
