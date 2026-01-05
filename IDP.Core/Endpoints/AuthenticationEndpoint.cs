namespace IDP.Core.Endpoints;

public class AuthenticationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        //var authGroup = app.MapGroup("/authenticate");

        //authGroup.MapPost("", async (AuthRequest request,
        //    IAppLogger<AuthenticationEndpoint> _logger,
        //    IAuthorizationCodeUseCase _identityService) =>
        //{
        //    _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

        //    var response = await _identityService.Authenticate(request);

        //    if (!response.IsSuccess)
        //    {
        //        var errorResult = ApiResult<ApiError>.Failure(
        //                        ApiError.Failure(response.Error));

        //        return Results.Json(errorResult, statusCode: StatusCodes.Status401Unauthorized);
        //    }

        //    _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

        //    return Results.Ok(response);
        //    ;
        //}).WithName("Authenticate")
        //.WithTags("Authentication");
    }
}
