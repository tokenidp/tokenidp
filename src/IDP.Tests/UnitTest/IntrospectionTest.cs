using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IDP.Tests.UnitTest;

public class IntrospectionTest
{
    private readonly Mock<IReferenceTokenValidator> MockTokenValidator;
    private readonly Mock<IAppLogger<IntrospectionController>> MockLogger;
    private readonly IntrospectionController Controller;

    public IntrospectionTest()
    {
        MockTokenValidator = new Mock<IReferenceTokenValidator>();
        MockLogger = new Mock<IAppLogger<IntrospectionController>>();
        Controller = new IntrospectionController(MockTokenValidator.Object, MockLogger.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Introspect_WhenTokenIsNullOrWhiteSpace_ReturnsBadRequest(string token)
    {
        // Arrange
        var request = new IntrospectionRequest { Token = token };

        // Act
        var result = await Controller.Introspect(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid request", badRequestResult.Value);
        MockLogger.Verify(x => x.LogWarning("Introspect called with invalid request"), Times.Once);
    }

    [Fact]
    public async Task Introspect_WhenTokenIsValid_CallsTokenValidator()
    {
        // Arrange
        var token = "valid.token.string";
        var request = new IntrospectionRequest { Token = token };
        var expectedResponse = IntrospectionResponse.Create(1, 1, "Profile", ["Admin"]);

        MockTokenValidator.Setup(x => x.ValidateReferenceToken(token))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Controller.Introspect(request);

        // Assert
        MockTokenValidator.Verify(x => x.ValidateReferenceToken(token), Times.Once);
        MockLogger.Verify(x => x.LogInfo(
            "Introspect called for token (partial): {TokenPartial}",
            It.Is<string>(s => s.EndsWith("..."))), Times.Once);
    }

    [Fact]
    public async Task Introspect_WhenTokenIsValid_ReturnsOkWithTokenResponse()
    {
        // Arrange
        var token = "valid.token.string";
        var request = new IntrospectionRequest { Token = token };
        var expectedResponse = IntrospectionResponse.Create(1, 1, "Profile", ["Admin"]);

        MockTokenValidator.Setup(x => x.ValidateReferenceToken(token))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await Controller.Introspect(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResponse, okResult.Value);
        MockLogger.Verify(x => x.LogInfo(
            "Introspect completed. Active: {IsActive}", expectedResponse.Active), Times.Once);
    }

    [Fact]
    public async Task Introspect_WhenTokenValidatorThrowsException_LogsAndRethrows()
    {
        // Arrange
        var token = "invalid.token.string";
        var request = new IntrospectionRequest { Token = token };
        var exception = new Exception("Validation failed");

        MockTokenValidator.Setup(x => x.ValidateReferenceToken(token))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => Controller.Introspect(request));

        MockLogger.Verify(x => x.LogError(
            It.IsAny<string>(), exception), Times.Once);
    }

    [Fact]
    public async Task Introspect_LogsPartialTokenForSecurity()
    {
        // Arrange
        var token = "full.token.string.12345";
        var request = new IntrospectionRequest { Token = token };
        var expectedResponse = IntrospectionResponse.Create(1, 1, "Profile", ["Admin"]);

        MockTokenValidator.Setup(x => x.ValidateReferenceToken(token))
            .ReturnsAsync(expectedResponse);

        // Act
        await Controller.Introspect(request);

        // Assert
        MockLogger.Verify(x => x.LogInfo(
            "Introspect called for token (partial): {TokenPartial}",
            "12345..."), Times.Once);
    }
}