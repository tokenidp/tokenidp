using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal sealed class CibaRequestsEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/backchannel-authentication/requests")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("/pending", async (
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetPendingAsync(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.view"
        })
        .WithName("CibaPendingRequests")
        .WithTags("CibaRequests");

        authGroup.MapPost("/{id:int}/approve", async (
            int id,
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.ApproveAsync(id, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.approve"
        })
        .WithName("CibaApproveRequest")
        .WithTags("CibaRequests");

        authGroup.MapPost("/{id:int}/deny", async (
            int id,
            [FromBody] CibaDenyRequest? request,
            CibaApprovalUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.DenyAsync(id, request?.Reason, httpContext.RequestAborted);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "ciba.deny"
        })
        .WithName("CibaDenyRequest")
        .WithTags("CibaRequests");
    }
}
