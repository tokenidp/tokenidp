using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.OAuth.Endpoints;

public class LoginEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/local-login", async (AuthorizationRequest request,
            IAppLogger<LoginEndpoint> _logger,
            IAuthorizationCodeUseCase _identityService) =>
        {
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
