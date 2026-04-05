using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IDP.Tests.UnitTest;

public class UserTest
{
    private readonly Mock<UserRepo> _mockUserService;
    private readonly Mock<IAppLogger<UserController>> _mockLogger;
    private readonly UserController _controller;

    public UserTest()
    {
        _mockUserService = new Mock<UserRepo>();
        _mockLogger = new Mock<IAppLogger<UserController>>();
        _controller = new UserController(_mockUserService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserInfo_ValidUserId_ReturnsUserInfo()
    {
        // Arrange
        const int userId = 123;
        var expectedUser = UserInfo.Create(1, 1, "nraza", string.Empty, default);

        _mockUserService.Setup(x => x.GetUserInfo(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUserInfo(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultValue = Assert.IsType<Result<UserInfo>>(okResult.Value);

        Assert.Equal(expectedUser, resultValue.Value);
        Assert.True(resultValue.IsSuccess);

        _mockLogger.Verify(x => x.LogInfo(
            "GetUserInfo called for userId: {UserId}", userId), Times.Once);

        _mockLogger.Verify(x => x.LogInfo(
            "GetUserInfo completed for userId: {UserId}", userId), Times.Once);
    }

    [Fact]
    public async Task GetUserInfo_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 999;
        _mockUserService.Setup(x => x.GetUserInfo(userId))
            .ReturnsAsync((UserInfo)null);

        // Act
        var result = await _controller.GetUserInfo(userId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
        _mockLogger.Verify(x => x.LogWarning(
            It.Is<string>(m => m.Contains("User not found")), Times.Once));
    }

    [Fact]
    public async Task GetUserInfo_ServiceThrowsException_LogsAndRethrows()
    {
        // Arrange
        const int userId = 123;
        var exception = new Exception("Database error");

        _mockUserService.Setup(x => x.GetUserInfo(userId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetUserInfo(userId));
        _mockLogger.Verify(x => x.LogError(
            It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task GetUserInfo_InvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        const int invalidUserId = 0;

        // Act
        var result = await _controller.GetUserInfo(invalidUserId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(x => x.LogWarning(
            It.Is<string>(m => m.Contains("Invalid user ID"))), Times.Once);
    }

    [Fact]
    public async Task GetUserInfo_VerifyServiceCalledExactlyOnce()
    {
        // Arrange
        const int userId = 123;
        _mockUserService.Setup(x => x.GetUserInfo(userId))
            .ReturnsAsync(UserInfo.Create(1, 1, "nraza", string.Empty, default));

        // Act
        await _controller.GetUserInfo(userId);

        // Assert
        _mockUserService.Verify(x => x.GetUserInfo(userId), Times.Once);
    }
}