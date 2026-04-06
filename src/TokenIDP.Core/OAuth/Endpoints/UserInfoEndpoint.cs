using TokenIDP.Core.OAuth.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class UserInfoEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/userinfo")
        .RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
        });

        authGroup.MapGet("", static async (HttpContext httpContext,
            UserInfoUseCase userInfoService) =>
        {
            return await userInfoService.HandleAsync(httpContext.RequestAborted);
        })
        .WithName("UserInfo")
        .WithTags("UserInfo");
    }
}

