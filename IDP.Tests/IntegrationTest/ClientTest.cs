using FluentAssertions;
using IDP.Tests.Infrastructure;
using Services.Common.Model;
using System.Net;
using System.Net.Http.Json;

namespace IDP.Tests.IntegrationTest;

public class ClientTest : IntegrationTestBase
{
    public ClientTest(IntegrationTestFixture fixture) : base(fixture)
    {

    }

    [Fact]
    public async Task IsValidClient_ShouldReturnTrue_WhenClientExists()
    {
        // Arrange
        string validClientId = "test-client-id";

        // Act
        var requestBuilder = NewRequest.AddRoute($"/client/{validClientId}");
        var response = await requestBuilder.Get(false);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>();

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}