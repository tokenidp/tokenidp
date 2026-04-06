using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal sealed class EmailConfirmationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth")
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/confirm-email", async (
            CompleteEmailConfirmationRequest request,
            EmailConfirmationUseCase emailConfirmationUseCase,
            HttpContext httpContext) =>
        {
            var command = new CompleteEmailConfirmationCommand
            {
                RawToken = request.Token
            };

            var response = await emailConfirmationUseCase
                .CompleteEmailConfirmation(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .AllowAnonymous()
        .WithName("CompleteEmailConfirmation")
        .WithTags("EmailConfirmation");
    }
}
