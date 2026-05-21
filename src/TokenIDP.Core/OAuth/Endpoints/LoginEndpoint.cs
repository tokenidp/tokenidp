using Microsoft.AspNetCore.Antiforgery;
using System.Text.Json;

namespace TokenIDP.Core.OAuth.Endpoints;

public class LoginEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
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
