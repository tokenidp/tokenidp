using IDP.Service.Application;
using IDP.Service.Controllers;
using IDP.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services.Common.Interfaces;
using Services.Common.Model;

namespace IDP.Tests.UnitTest;

public class ClientTest
{
    private readonly Mock<ClientService> _mockClientService;
    private readonly Mock<IAppLogger<ClientController>> _mockLogger;
    private readonly ClientController _controller;

    public ClientTest()
    {
        _mockClientService = new Mock<ClientService>();
        _mockLogger = new Mock<IAppLogger<ClientController>>();
        _controller = new ClientController(_mockClientService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task IsValidClient_WhenClientIdIsValid_ReturnsTrue()
    {
        // Arrange
        const string clientId = "test-client-123";
        _mockClientService.Setup(x => x.IsValidClient(clientId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.IsValidClient(clientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<bool>>(okResult.Value);
        Assert.True(resultValue.Value);

        _mockLogger.Verify(x => x.LogInfo(
            "IsValidClient called for clientId: {ClientId}", clientId), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "IsValidClient result for clientId: {ClientId} is {Result}",
            clientId, true), Times.Once);
    }

    [Fact]
    public async Task IsValidClient_WhenClientIdIsInvalid_ReturnsFalse()
    {
        // Arrange
        const string clientId = "invalid-client";
        _mockClientService.Setup(x => x.IsValidClient(clientId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.IsValidClient(clientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<bool>>(okResult.Value);
        Assert.False(resultValue.Value);
    }

    [Fact]
    public async Task IsValidClient_WhenClientIdIsEmpty_ReturnsFalse()
    {
        // Arrange
        const string clientId = "";
        _mockClientService.Setup(x => x.IsValidClient(clientId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.IsValidClient(clientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<bool>>(okResult.Value);
        Assert.False(resultValue.Value);
    }

    [Fact]
    public async Task IsValidClient_WhenServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        const string clientId = "error-client";
        var exception = new Exception("Test error");
        _mockClientService.Setup(x => x.IsValidClient(clientId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.IsValidClient(clientId));

        _mockLogger.Verify(x => x.LogError(
            It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task IsValidClient_VerifiesServiceCalledExactlyOnce()
    {
        // Arrange
        const string clientId = "test-client";
        _mockClientService.Setup(x => x.IsValidClient(clientId))
            .ReturnsAsync(true);

        // Act
        await _controller.IsValidClient(clientId);

        // Assert
        _mockClientService.Verify(x => x.IsValidClient(clientId), Times.Once);
    }
}