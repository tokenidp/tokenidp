using TokenIDP.Domain.AggregateRoots.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class AuthorizeEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/authorize", async (
            HttpContext httpContext,
            IAuthorizationRequestValidator authorizationValidator,
            IAuthorizationCodeUseCase authorizationCodeUseCase,
            IAuthorizationRepository authorizationStore) =>
        {
            var ctx = httpContext.Request.Query["ctx"].ToString();

            if (!string.IsNullOrWhiteSpace(ctx))
            {
                return await ResumeAuthorization(
                    httpContext,
                    ctx,
                    authorizationCodeUseCase,
                    authorizationStore);
            }

            return await StartAuthorization(
                httpContext,
                authorizationValidator,
                authorizationCodeUseCase,
                authorizationStore);
        });
    }

    private static async Task<IResult> ResumeAuthorization(
        HttpContext httpContext,
        string ctx,
        IAuthorizationCodeUseCase authorizationCodeUseCase,
        IAuthorizationRepository authorizationStore)
    {
        var existing = await authorizationStore.GetPreAuthorization(ctx);

        if (existing == null)
            return Results.BadRequest("Invalid authorization context.");

        var authResult = await httpContext.AuthenticateAsync("idp_session");

        if (!authResult.Succeeded || authResult.Principal?.Identity?.IsAuthenticated != true)
            return Results.Redirect(QueryHelpers.AddQueryString("/login", "ctx", ctx));

        var tenantClaim = authResult.Principal.FindFirst("uid")?.Value;

        if (!int.TryParse(tenantClaim, out var userTenantId))
            return Results.BadRequest("Invalid tenant claim.");

        if (userTenantId != existing.TenantId)
            return Results.BadRequest("Tenant mismatch.");

        var userId = authResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Results.BadRequest("ResumeAuthorization: User id missing.");

        var authRequest = new AuthorizationRequest
        {
            ClientId = existing.ClientId!,
            RedirectUri = existing.RedirectUri!,
            ResponseType = existing.GrantType ?? "code",
            Scopes = existing.Scopes!,
            CodeChallenge = existing.CodeChallenge!,
            CodeChallengeMethod = existing.CodeChallengeMethod!
        };

        return await IssueAuthorizationCodeForSession(
            authResult.Principal,
            authRequest,
            existing.State!,
            authorizationCodeUseCase);
    }

    private static async Task<IResult> StartAuthorization(
        HttpContext httpContext,
        IAuthorizationRequestValidator authorizationValidator,
        IAuthorizationCodeUseCase authorizationCodeUseCase,
        IAuthorizationRepository authorizationStore)
    {
        var query = httpContext.Request.Query;

        var authorizationRequest = new AuthorizationRequest
        {
            ClientId = query["client_id"].ToString(),
            RedirectUri = query["redirect_uri"].ToString(),
            ResponseType = query["response_type"].ToString(),
            Scopes = query["scope"].ToString(),
            CodeChallenge = query["code_challenge"].ToString(),
            CodeChallengeMethod = query["code_challenge_method"].ToString()
        };

        var state = query["state"].ToString();

        if (string.IsNullOrWhiteSpace(authorizationRequest.ClientId) ||
            string.IsNullOrWhiteSpace(authorizationRequest.RedirectUri) ||
            string.IsNullOrWhiteSpace(authorizationRequest.ResponseType) ||
            string.IsNullOrWhiteSpace(authorizationRequest.Scopes))
        {
            return Results.BadRequest("Missing required OAuth parameters.");
        }

        if (!string.Equals(authorizationRequest.ResponseType, "code", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Only response_type=code is supported.");

        if (string.IsNullOrWhiteSpace(authorizationRequest.CodeChallenge) ||
            string.IsNullOrWhiteSpace(authorizationRequest.CodeChallengeMethod))
        {
            return Results.BadRequest("Missing PKCE parameters.");
        }

        ClientShortInfo clientShortInfo;

        try
        {
            clientShortInfo = await authorizationValidator.ValidateAsync(
                authorizationRequest,
                httpContext.RequestAborted);
        }
        catch (AuthorizationRequestException ex)
        {
            return Results.BadRequest(new
            {
                error = ex.Error,
                error_description = ex.ErrorDescription
            });
        }

        var authResult = await httpContext.AuthenticateAsync("idp_session");

        if (authResult.Succeeded && authResult.Principal?.Identity?.IsAuthenticated == true)
        {
            return await IssueAuthorizationCodeForSession(
                authResult.Principal,
                authorizationRequest,
                state,
                authorizationCodeUseCase);
        }

        return await CreateLoginRedirect(
            httpContext,
            authorizationRequest,
            state,
            clientShortInfo,
            authorizationStore);
    }

    private static async Task<IResult> IssueAuthorizationCodeForSession(
        ClaimsPrincipal claimsPrincipal,
        AuthorizationRequest authorizationRequest,
        string state,
        IAuthorizationCodeUseCase authorizationCodeUseCase)
    {
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Results.BadRequest("IssueAuthorizationCodeForSession: User id missing.");

        var authCode = await authorizationCodeUseCase
            .GenerateAuthorizationCode(authorizationRequest, Convert.ToInt32(userId));

        if (authCode == null || !authCode.IsSuccess)
            return Results.BadRequest("Authorization code generation failed.");

        var parameters = new Dictionary<string, string?>
        {
            ["code"] = authCode.AuthorizationCode
        };

        if (!string.IsNullOrWhiteSpace(state))
            parameters["state"] = state;

        return Results.Redirect(QueryHelpers.AddQueryString(
            authorizationRequest.RedirectUri!, parameters));
    }

    private static async Task<IResult> CreateLoginRedirect(
        HttpContext httpContext,
        AuthorizationRequest authorizationRequest,
        string state,
        ClientShortInfo clientShortInfo,
        IAuthorizationRepository authorizationStore)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var preAuthorization = new PreAuthorization(
            clientShortInfo.TenantId,
            correlationId,
            clientShortInfo.Id,
            authorizationRequest.ClientId!,
            authorizationRequest.RedirectUri!,
            authorizationRequest.CodeChallenge!,
            authorizationRequest.CodeChallengeMethod!,
            authorizationRequest.ResponseType!,
            state,
            authorizationRequest.Scopes!);

        await authorizationStore.CreatePreAuthorization(
            preAuthorization,
            httpContext.RequestAborted);

        var loginUrl = QueryHelpers.AddQueryString("/login", "ctx", correlationId);

        return Results.Redirect(loginUrl);
    }
}

