using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Admin.Activities.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal class ActivityEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/activity")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/list", async (SearchData data,
            ActivityQueryUseCase useCase) =>
        {
            var response = await useCase.GetActivities(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "activities.view"
        })
        .WithName("Activities")
        .WithTags("Activities");

        authGroup.MapGet("/lookups", async (ActivityLookupsUseCase useCase,
           HttpContext httpContext) =>
        {
            var response = await useCase.GetLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            Policy = "activities.view"
        })
       .WithName("ActivityLookups")
       .WithTags("Activities");
    }
}

