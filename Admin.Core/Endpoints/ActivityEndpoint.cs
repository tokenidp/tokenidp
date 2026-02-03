using Admin.Core.Activities.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

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
        .WithName("Activities")
        .WithTags("Activities");

        authGroup.MapGet("/lookups", async (ActivityLookupsUseCase useCase,
           HttpContext httpContext) =>
        {
            var response = await useCase.GetLookups(httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
       .WithName("ActivityLookups")
       .WithTags("ActivityLookups");
    }
}
