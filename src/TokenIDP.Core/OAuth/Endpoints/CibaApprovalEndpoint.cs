using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class CibaApprovalEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/backchannel-authentication/requests")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},idp_session"
            });

        authGroup.MapGet("/pending", async (
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var result = await useCase.GetPendingAsync(httpContext.RequestAborted);
            return Results.Ok(result);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.view"
        })
        .WithName("CibaPendingRequests")
        .WithTags("BackchannelAuthentication");

        authGroup.MapPost("/{id:int}/approve", async (
            int id,
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var result = await useCase.ApproveAsync(id, httpContext.RequestAborted);
            return Results.Ok(result);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.approve"
        })
        .WithName("ApproveCibaRequest")
        .WithTags("BackchannelAuthentication");

        authGroup.MapPost("/{id:int}/deny", async (
            int id,
            CibaDenyRequest request,
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var result = await useCase.DenyAsync(id, request.Reason, httpContext.RequestAborted);
            return Results.Ok(result);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.deny"
        })
        .WithName("DenyCibaRequest")
        .WithTags("BackchannelAuthentication");
    }
}
