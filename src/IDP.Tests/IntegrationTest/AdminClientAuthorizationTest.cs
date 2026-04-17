using FluentAssertions;
using IDP.Tests.Infrastructure;
using System.Net;

namespace IDP.Tests.IntegrationTest;

public class AdminClientAuthorizationTest : IntegrationTestBase
{
    public AdminClientAuthorizationTest(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DeleteClient_ShouldReturnUnauthorized_WhenNoTokenProvided()
    {
        var requestBuilder = NewRequest.AddRoute("/admin/client/1");
        var response = await requestBuilder.Delete(false);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
