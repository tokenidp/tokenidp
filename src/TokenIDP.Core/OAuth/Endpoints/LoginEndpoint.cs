using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Endpoints;

public class LoginEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/local-login/form", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<LoginEndpoint> logger,
            IAuthorizationRepository authorizationStore,
            IAuthorizationCodeUseCase identityService) =>
        {
            var ctx = httpContext.Request.Query["ctx"].ToString();
            var loginUrl = BuildLoginUrl(ctx);

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning(
                    "Local login form rejected by antiforgery validation. Path={Path}, HasFormToken={HasFormToken}, HasCookies={HasCookies}, Error={Error}",
                    httpContext.Request.Path.Value ?? string.Empty,
                    httpContext.Request.HasFormContentType,
                    httpContext.Request.Cookies.Count > 0,
                    ex.Message);

                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            if (string.IsNullOrWhiteSpace(ctx))
            {
                logger.LogWarning("Local login form rejected because authorization context id was missing.");
                return Results.BadRequest("Missing authorization context.");
            }

            var preAuthorization = await authorizationStore.GetPreAuthorization(ctx);
            if (preAuthorization is null)
            {
                logger.LogWarning(
                    "Local login form rejected because authorization context was invalid or expired. AuthorizationContextId={AuthorizationContextId}",
                    ctx);

                return Results.BadRequest("Authorization context is invalid or expired.");
            }

            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var userName = form["username"].ToString();
            var password = form["password"].ToString();
            var rememberMe = string.Equals(form["rememberMe"].ToString(), "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(form["rememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Local login form rejected because username or password was missing. AuthorizationContextId={AuthorizationContextId}, HasUsername={HasUsername}, HasPassword={HasPassword}",
                    ctx,
                    !string.IsNullOrWhiteSpace(userName),
                    !string.IsNullOrWhiteSpace(password));

                return Results.Redirect(BuildLoginUrl(ctx, "Username and password are required."));
            }

            var request = new AuthorizationRequest
            {
                UserName = userName,
                Password = password,
                ClientId = preAuthorization.ClientId!,
                CodeChallenge = preAuthorization.CodeChallenge!,
                RedirectUri = preAuthorization.RedirectUri!,
                CodeChallengeMethod = preAuthorization.CodeChallengeMethod!,
                Scopes = preAuthorization.Scopes!,
                RememberMe = rememberMe,
                AuthorizationContextId = preAuthorization.CorrelationId!,
                TenantId = preAuthorization.TenantId
            };

            logger.LogInfo(
                "Local login form submitted. UserName={UserName}, TenantId={TenantId}, AuthorizationContextId={AuthorizationContextId}",
                request.UserName,
                request.TenantId,
                request.AuthorizationContextId);

            var response = await identityService.Authenticate(request);

            if (!response.IsSuccess)
            {
                logger.LogWarning(
                    "Local login form failed. UserName={UserName}, TenantId={TenantId}, AuthorizationContextId={AuthorizationContextId}, Error={Error}",
                    request.UserName,
                    request.TenantId,
                    request.AuthorizationContextId,
                    response.Error);

                return Results.Redirect(BuildLoginUrl(ctx, response.Error ?? "Invalid login."));
            }

            logger.LogInfo(
                "Local login form completed. UserName={UserName}, TenantId={TenantId}, AuthorizationContextId={AuthorizationContextId}, TwoFactorEnabled={TwoFactorEnabled}",
                request.UserName,
                request.TenantId,
                request.AuthorizationContextId,
                response.TwoFactorEnabled == true);

            if (response.TwoFactorEnabled == true)
            {
                var mfaUrl = QueryHelpers.AddQueryString(
                    "/mfa",
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["uid"] = response.UserId?.ToString(),
                        ["corid"] = response.CorrelationId,
                        ["flow"] = "authorize",
                        ["redirect_uri"] = preAuthorization.RedirectUri
                    });

                return Results.Redirect(mfaUrl);
            }

            return Results.Redirect(QueryHelpers.AddQueryString("/authorize", "ctx", ctx));

            static string BuildLoginUrl(string authorizationContextId, string? errorDescription = null)
            {
                var query = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["ctx"] = authorizationContextId
                };

                if (!string.IsNullOrWhiteSpace(errorDescription))
                {
                    query["error_description"] = errorDescription;
                }

                return QueryHelpers.AddQueryString("/login", query);
            }
        })
        .AllowAnonymous()
        .WithName("LocalLoginForm")
        .WithTags("LocalLogin");

        app.MapPost("/local-login", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<LoginEndpoint> _logger,
            IAuthorizationCodeUseCase _identityService) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                _logger.LogWarning(
                    "Local login rejected by antiforgery validation. Path={Path}, HasXsrfHeader={HasXsrfHeader}, HasCookies={HasCookies}, Error={Error}",
                    httpContext.Request.Path.Value ?? string.Empty,
                    httpContext.Request.Headers.ContainsKey("X-XSRF-TOKEN"),
                    httpContext.Request.Cookies.Count > 0,
                    ex.Message);

                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Invalid or missing antiforgery token.")));
            }

            AuthorizationRequest? request;

            try
            {
                request = await httpContext.Request.ReadFromJsonAsync<AuthorizationRequest>();
            }
            catch (Exception ex) when (ex is BadHttpRequestException or JsonException)
            {
                _logger.LogWarning(
                    "Local login rejected because the request body could not be read. Path={Path}, ContentType={ContentType}, Error={Error}",
                    httpContext.Request.Path.Value ?? string.Empty,
                    httpContext.Request.ContentType ?? string.Empty,
                    ex.Message);

                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Invalid login request.")));
            }

            if (request is null ||
                string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning(
                    "Local login rejected because username or password was missing. Path={Path}, HasRequest={HasRequest}, HasUsername={HasUsername}, HasPassword={HasPassword}",
                    httpContext.Request.Path.Value ?? string.Empty,
                    request is not null,
                    !string.IsNullOrWhiteSpace(request?.UserName),
                    !string.IsNullOrWhiteSpace(request?.Password));

                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Username and password are required.")));
            }

            _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

            var response = await _identityService.Authenticate(request);

            if (!response.IsSuccess)
            {
                var errorResult = ApiResult<ApiError>.Failure(
                                ApiError.Failure(response.Error));

                return Results.Json(errorResult, statusCode: StatusCodes.Status401Unauthorized);
            }

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return Results.Ok(response);

        })
        .AllowAnonymous()
        .WithName("LocalLogin")
        .WithTags("LocalLogin");
    }
}
