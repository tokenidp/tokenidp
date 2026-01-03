
namespace IDP.Core.Endpoints;

internal class UserInfoEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/userinfo")
            .RequireAuthorization();

        authGroup.MapGet("/", static async (HttpContext httpContext,
            UserInfoService userInfoService) =>
        {
            await userInfoService.HandleAsync(httpContext, httpContext.RequestAborted);
            return Results.Empty;
        })
        .WithName("UserInfo")
        .WithTags("UserInfo");
    }
}
