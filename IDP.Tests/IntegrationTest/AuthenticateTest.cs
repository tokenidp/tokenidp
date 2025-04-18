using FluentAssertions;
using IDP.Service.Model;
using IDP.Tests.Infrastructure;
using Services.Common.Model;
using System.Net;
using System.Net.Http.Json;

namespace IDP.Tests.IntegrationTest;

public class AuthenticateTest : IntegrationTestBase
{
    public AuthenticateTest(IntegrationTestFixture fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task Authenticate_ShouldReturnSuccess_WhenValidCredentialsProvided()
    {
        // Arrange
        var request = new AuthRequest
        {
            UserName = "testuser",
            Password = "Test@1234"
        };

        // Act
        var requestBuilder = NewRequest.AddRoute("/authenticate");
        var response = await requestBuilder.Post(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthResponse>>();

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}