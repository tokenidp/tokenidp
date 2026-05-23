using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Text.RegularExpressions;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.Endpoints;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.Base;

namespace TokenIDP.Tests.OAuth;

public sealed class CibaApprovalEndpointTests
{
    [Fact]
    public async Task GetApprove_WithValidMagicLink_ShowsApprovalPageWithoutLoginRedirect()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var response = await client.GetAsync($"/ciba/approve?requestId={request.PublicRequestId:D}&token={WebUtility.UrlEncode(token)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Approve sign-in request");
        body.Should().Contain(">Approve</button>");
        body.Should().Contain(">Reject</button>");
        body.Should().NotContain("/login");
        body.Should().NotContain("/ciba/login");
    }

    [Fact]
    public async Task GetApprove_WithInvalidToken_DoesNotShowApprovalButtons()
    {
        var request = CibaTestData.CreatePendingRequest(approvalToken: "approval-token");
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var response = await client.GetAsync($"/ciba/approve?requestId={request.PublicRequestId:D}&token=wrong-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Approval link unavailable");
        body.Should().NotContain(">Approve</button>");
        body.Should().NotContain(">Reject</button>");
    }

    [Fact]
    public async Task GetApprove_WithExpiredToken_DoesNotShowApprovalButtons()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        CibaTestData.SetProperty(request, nameof(BackchannelAuthenticationRequest.ApprovalTokenExpiresAtUtc), DateTime.UtcNow.AddSeconds(-1));
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var response = await client.GetAsync($"/ciba/approve?requestId={request.PublicRequestId:D}&token={WebUtility.UrlEncode(token)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Approval link unavailable");
        body.Should().NotContain(">Approve</button>");
        body.Should().NotContain(">Reject</button>");
    }

    [Fact]
    public async Task PostApprove_ConsumesTokenAndMarksRequestApproved()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var antiforgery = await GetAntiforgeryTokenAsync(client, request, token);
        var response = await PostDecisionAsync(client, antiforgery, request, token, "approve");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        request.Status.Should().Be(CibaRequestStatus.Approved);
        request.ApprovalTokenConsumedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task PostApprove_ConsumesTokenAndMarksRequestRejected()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var antiforgery = await GetAntiforgeryTokenAsync(client, request, token);
        var response = await PostDecisionAsync(client, antiforgery, request, token, "reject");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        request.Status.Should().Be(CibaRequestStatus.Denied);
        request.ApprovalTokenConsumedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task PostApprove_CannotReuseConsumedToken()
    {
        const string token = "approval-token";
        var request = CibaTestData.CreatePendingRequest(approvalToken: token);
        using var server = CreateServer(request);
        using var client = server.CreateClient();

        var antiforgery = await GetAntiforgeryTokenAsync(client, request, token);
        await PostDecisionAsync(client, antiforgery, request, token, "approve");

        var secondToken = await GetAntiforgeryTokenAsync(client, request, token, expectSuccess: false);
        secondToken.RequestToken.Should().BeEmpty();
    }

    [Fact]
    public void RegisterEndpoints_DoesNotMapCibaLoginRoutes()
    {
        using var server = CreateServer(CibaTestData.CreatePendingRequest(approvalToken: "approval-token"));
        var endpoints = server.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToArray();

        endpoints.Should().Contain("/ciba/approve");
        endpoints.Should().NotContain("/ciba/login");
    }

    private static async Task<HttpResponseMessage> PostDecisionAsync(
        HttpClient client,
        AntiforgeryResponse antiforgery,
        BackchannelAuthenticationRequest request,
        string token,
        string decision)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/ciba/approve")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgery.RequestToken,
                ["requestId"] = request.PublicRequestId.ToString("D"),
                ["token"] = token,
                ["decision"] = decision
            })
        };
        message.Headers.TryAddWithoutValidation("Cookie", antiforgery.Cookie);
        return await client.SendAsync(message);
    }

    private static async Task<AntiforgeryResponse> GetAntiforgeryTokenAsync(
        HttpClient client,
        BackchannelAuthenticationRequest request,
        string token,
        bool expectSuccess = true)
    {
        var response = await client.GetAsync($"/ciba/approve?requestId={request.PublicRequestId:D}&token={WebUtility.UrlEncode(token)}");
        var body = await response.Content.ReadAsStringAsync();

        if (!expectSuccess)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            body.Should().NotContain("__RequestVerificationToken");
            return new AntiforgeryResponse(string.Empty, string.Empty);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var match = Regex.Match(body, "name=\"__RequestVerificationToken\" value=\"(?<token>[^\"]+)\"");
        match.Success.Should().BeTrue();
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? string.Join("; ", cookies.Select(x => x.Split(';')[0]))
            : string.Empty;

        cookie.Should().NotBeNullOrWhiteSpace();
        return new AntiforgeryResponse(WebUtility.HtmlDecode(match.Groups["token"].Value), cookie);
    }

    private sealed record AntiforgeryResponse(string RequestToken, string Cookie);

    private static TestServer CreateServer(BackchannelAuthenticationRequest request)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
                services.AddScoped(_ => CreateUseCase(request));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => new CibaApprovalEndpoint().RegisterEndpoints(endpoints));
            });

        return new TestServer(builder);
    }

    private static CibaApprovalUseCase CreateUseCase(BackchannelAuthenticationRequest request)
    {
        var authorizationRepository = new Mock<IAuthorizationRepository>();
        var clientRepository = new Mock<IClientRepository>();
        var eventDispatcher = new Mock<IApplicationEventDispatcher>();

        authorizationRepository
            .Setup(x => x.GetBackchannelAuthenticationRequestByPublicIdAsync(request.PublicRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        authorizationRepository
            .Setup(x => x.UpdateBackchannelAuthenticationRequest(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request.Id);

        clientRepository
            .Setup(x => x.GetClientShortInfo(request.ClientId))
            .ReturnsAsync(new ClientShortInfo(
                id: 1,
                tenantId: request.TenantId,
                allowForgotPassword: false,
                clientName: "CIBA Client",
                redirectUri: "https://client.example/callback",
                requiredPkce: false,
                scopes: new[] { "openid", "profile" },
                grantTypes: new[] { GrantTypes.ciba }));

        eventDispatcher
            .Setup(x => x.RaiseAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new CibaApprovalUseCase(
            authorizationRepository.Object,
            clientRepository.Object,
            new TestCurrentUserService(),
            eventDispatcher.Object);
    }
}
