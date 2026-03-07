using IDP.Domain.AggregateRoots.Authorization;
using IDP.Foundation.Abstractions.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

namespace IDP.Core.Endpoints;

internal class AuthorizeEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/authorize", async (
            HttpContext httpContext,
            IAuthorizationRequestValidator authorizationValidator,
            IAuthorizationCodeUseCase authorizationCodeUseCase,
            IAuthorizationStore authorizationStore) =>
        {
            var query = httpContext.Request.Query;

            var ctx = query["ctx"].ToString();
            if (!string.IsNullOrWhiteSpace(ctx))
            {
                var existing = await authorizationStore.GetPreAuthorization(ctx);

                if (existing == null)
                    return Results.BadRequest("Invalid authorization context.");

                var authResult = await httpContext.AuthenticateAsync("idp_session");

                if (authResult.Succeeded && authResult.Principal?.Identity?.IsAuthenticated == true)
                {
                    var authRequest = new AuthorizationRequest
                    {
                        ClientId = existing.ClientId!,
                        RedirectUri = existing.RedirectUri!,
                        ResponseType = existing.GrantType ?? "code",
                        Scopes = existing.Scopes!,
                        CodeChallenge = existing.CodeChallenge!,
                        CodeChallengeMethod = existing.CodeChallengeMethod!
                    };

                    var userId = authResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(userId))
                        return Results.BadRequest("User id missing.");

                    var authCode = await authorizationCodeUseCase.GenerateAuthorizationCode(authRequest, Convert.ToInt32(userId));

                    if (authCode != null && authCode.IsSuccess)
                    {
                        var parameters = new Dictionary<string, string?>(StringComparer.Ordinal)
                        {
                            ["code"] = authCode?.AuthorizationCode
                        };

                        if (!string.IsNullOrWhiteSpace(existing.State!))
                            parameters["state"] = existing.State!;

                        return Results.Redirect(QueryHelpers.AddQueryString(existing.RedirectUri!, parameters));
                    }
                }

                return Results.Redirect(QueryHelpers.AddQueryString("/login", "ctx", ctx));
            }

            var clientId = query["client_id"].ToString();
            var redirectUri = query["redirect_uri"].ToString();
            var responseType = query["response_type"].ToString();
            var scopes = query["scope"].ToString();
            var codeChallenge = query["code_challenge"].ToString();
            var codeChallengeMethod = query["code_challenge_method"].ToString();
            var state = query["state"].ToString();

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(redirectUri) ||
                string.IsNullOrWhiteSpace(responseType) ||
                string.IsNullOrWhiteSpace(scopes))
            {
                return Results.BadRequest("Missing required OAuth parameters.");
            }

            if (!string.Equals(responseType, "code", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only response_type=code is supported.");
            }

            if (string.IsNullOrWhiteSpace(codeChallenge) ||
                string.IsNullOrWhiteSpace(codeChallengeMethod))
            {
                return Results.BadRequest("Missing PKCE parameters.");
            }

            var authorizationRequest = new AuthorizationRequest
            {
                ClientId = clientId,
                RedirectUri = redirectUri,
                ResponseType = responseType,
                Scopes = scopes,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod
            };

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

            var correlationId = Guid.NewGuid().ToString("N");

            var preAuthorization = new PreAuthorization(
                clientShortInfo.TenantId,
                correlationId,
                clientShortInfo.Id,
                clientId,
                redirectUri,
                codeChallenge,
                codeChallengeMethod,
                responseType,
                state,
                scopes);

            await authorizationStore.CreatePreAuthorization(preAuthorization, httpContext.RequestAborted);

            var loginUrl = QueryHelpers.AddQueryString("/login", "ctx", correlationId);

            return Results.Redirect(loginUrl);
        });
    }
}