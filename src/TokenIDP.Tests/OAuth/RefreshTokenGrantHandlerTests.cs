using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.GrantHandlers;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.Security;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Domain.AggregateRoots.Tokens;
using TokenIDP.Domain.AggregateRoots.Users;

namespace TokenIDP.Tests.OAuth;

public sealed class RefreshTokenGrantHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAcceptRefreshTokenFromCookie_WhenBodyValueIsMissing()
    {
        var rawRefreshToken = "cookie-refresh-token";
        var tokenSecretGenerator = new TokenSecretGenerator();
        var existingToken = CreateExistingToken(rawRefreshToken, tokenSecretGenerator);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"tt_refresh={rawRefreshToken}";

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var tokenRepository = new Mock<ITokenRepository>();
        tokenRepository
            .Setup(x => x.GetRefreshToken(It.IsAny<byte[]>()))
            .ReturnsAsync(existingToken);
        tokenRepository
            .Setup(x => x.CreateToken(It.IsAny<Token>()))
            .ReturnsAsync(1);
        tokenRepository
            .Setup(x => x.RemoveOldRefreshTokens(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        var clientRepository = new Mock<IClientRepository>();
        clientRepository
            .Setup(x => x.GetActiveByClientId("client-app"))
            .ReturnsAsync(CreateClientSnapshot(RefreshTokenDeliveryMode.Cookie));

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository
            .Setup(x => x.GetUserRoles(42))
            .ReturnsAsync(["admin"]);

        var userRepository = new Mock<IUserRepository>();
        var tenantRepository = new Mock<ITenantRepository>();
        userRepository
            .Setup(x => x.GetUserShortInfo(42))
            .ReturnsAsync(new UserShortInfo(
                id: 42,
                tenantId: 1,
                fullName: "Alice Example",
                email: "alice@example.com",
                emailConfirmed: true,
                userName: "alice",
                firstName: "Alice",
                lastName: "Example",
                phoneNumber: "0000000000",
                phoneNumberVerified: true,
                createdOn: DateTime.UtcNow.AddDays(-30),
                updatedOn: null));
        tenantRepository
            .Setup(x => x.GetSummaryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSummary
            {
                Id = 1,
                TenantKey = "system",
                TenantName = "System"
            });

        var tokenContextUseCase = new TokenContextUseCase(
            roleRepository.Object,
            clientRepository.Object,
            tenantRepository.Object,
            Mock.Of<IAppLogger<TokenContextUseCase>>(),
            userRepository.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.IpAddress).Returns((string?)"127.0.0.1");

        var jwtTokenGenerator = new JwtTokenGenerator(Options.Create(new TokenOptions
        {
            Issuer = "https://issuer.example",
            Audience = "tokenidp",
            Key = CreateTestSigningKey()
        }), currentUserService.Object);

        var tokenIssuerUseCase = new TokenIssuerUseCase(
            jwtTokenGenerator,
            Mock.Of<IAppLogger<TokenIssuerUseCase>>(),
            tokenRepository.Object,
            currentUserService.Object,
            tokenSecretGenerator);

        var cookieService = new RefreshTokenCookieService(
            Options.Create(new RefreshTokenCookieOptions()));

        var sut = new RefreshTokenGrantHandler(
            httpContextAccessor,
            Mock.Of<IAppLogger<RefreshTokenGrantHandler>>(),
            cookieService,
            tokenRepository.Object,
            tokenContextUseCase,
            tokenIssuerUseCase,
            tokenSecretGenerator);

        var result = await sut.HandleAsync(new TokenRequest
        {
            ClientId = "client-app",
            GrantType = "refresh_token",
            Scope = "openid offline_access"
        });

        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokenRepository.Verify(x => x.GetRefreshToken(It.IsAny<byte[]>()), Times.Once);
        tokenRepository.Verify(x => x.CreateToken(It.IsAny<Token>()), Times.Once);
    }

    private static Token CreateExistingToken(
        string rawRefreshToken,
        TokenSecretGenerator tokenSecretGenerator)
    {
        var context = TokenContext.Create(
            userId: 42,
            tenantId: 1,
            clientName: "Client App",
            userName: "alice",
            clientId: "client-app",
            grantType: GrantTypes.authorization_code,
            tokenType: TokenTypes.JWT,
            clientSecretExpiry: 0,
            accessTokenLifetime: 60,
            refreshTokenExpiration: 30,
            refreshTokenDeliveryMode: RefreshTokenDeliveryMode.Cookie,
            rememberMe: true,
            roles: ["admin"],
            scopes: ["openid", "offline_access"],
            audiences: ["tokenidp"]);

        context.SetTokenDates();
        context.SetRefreshTokenExpiry();

        var token = Token.CreateToken(context);
        token.AddRefreshToken(
            DateTime.UtcNow.AddDays(7),
            tokenSecretGenerator.HashToken(rawRefreshToken),
            "127.0.0.1",
            "Client App",
            "alice");

        return token;
    }

    private static ClientValidationSnapshot CreateClientSnapshot(
        RefreshTokenDeliveryMode refreshTokenDeliveryMode)
    {
        return new ClientValidationSnapshot(
            clientId: "client-app",
            clientName: "Client App",
            tenantId: 1,
            isActive: true,
            redirectUri: "https://app.example/callback",
            logoutRedirectUri: "https://app.example/logout",
            clientType: ClientTypes.SPA,
            tokenType: TokenTypes.JWT,
            grantTypes: [GrantTypes.authorization_code, GrantTypes.refresh_token],
            scopes: ["openid", "offline_access"],
            apiResources: ["tokenidp"],
            apiScopeAssignments: [new ClientApiScopeAssignment("api.read", "tokenidp")],
            activeSecretHashes: Array.Empty<string>(),
            accessTokenLifetime: 60,
            authorizationCodeLifetime: 5,
            refreshTokenExpiration: 30,
            refreshTokenDeliveryMode: refreshTokenDeliveryMode,
            clientSecretExpiry: null,
            cibaEnabled: false,
            backchannelTokenDeliveryMode: CibaTokenDeliveryModes.Poll,
            cibaDefaultExpirySeconds: 300,
            cibaMinIntervalSeconds: 5,
            requireCibaUserCode: false,
            allowCibaLoginHint: true,
            allowCibaLoginHintToken: false,
            allowCibaIdTokenHint: false);
    }

    private static string CreateTestSigningKey()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportPkcs8PrivateKeyPem();
    }
}
