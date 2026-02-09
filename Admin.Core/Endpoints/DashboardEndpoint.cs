using Admin.Core.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

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
            HttpContext httpContext) =>
        {
            var response = await useCase.GetDashboard(CancellationToken.None);

            return EndpointResultMapper.ToOkOrError(response);

        })
         .WithName("Dashboard")
         .WithTags("Dashboard");
    }
}
