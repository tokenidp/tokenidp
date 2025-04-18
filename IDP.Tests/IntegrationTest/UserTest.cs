using FluentAssertions;
using IDP.Tests.Infrastructure;
using System.Net;

namespace IDP.Tests.IntegrationTest;

public class UserTest : IntegrationTestBase
{
    public UserTest(IntegrationTestFixture fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnUnauthorized_WhenNoTokenProvided()
    {
        // Arrange
        int userId = 1;

        // Act
        var requestBuilder = NewRequest.AddRoute($"/userinfo/{userId}");
        var response = await requestBuilder.Get(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}