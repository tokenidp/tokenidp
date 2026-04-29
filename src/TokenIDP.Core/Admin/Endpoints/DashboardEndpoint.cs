using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Admin.Dashboard;

namespace TokenIDP.Core.Admin.Endpoints;

internal class DashboardEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/dashboard")
             .RequireAuthorization(new AuthorizeAttribute
             {
                 AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
             })
             .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("", async (DashboardQueryUseCase useCase,
            string? period,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetDashboard(period, CancellationToken.None);

            return EndpointResultMapper.ToOkOrError(response);

        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "dashboard.view"
         })
         .WithName("Dashboard")
         .WithTags("Dashboard");
    }
}

