using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal sealed class PasswordResetEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth")
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            PasswordResetUseCase passwordResetUseCase,
            HttpContext httpContext) =>
        {
            var command = new InitiateSelfServicePasswordResetCommand
            {
                Email = request.Email,
                ClientId = request.ClientId
            };

            var response = await passwordResetUseCase
                .InitiateSelfServicePasswordReset(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .AllowAnonymous()
        .WithName("SelfPasswordReset")
        .WithTags("PasswordReset");

        authGroup.MapPost("/reset-password", async (
           CompletePasswordResetRequest request,
           PasswordResetUseCase passwordResetUseCase,
           HttpContext httpContext) =>
        {
            var command = new CompletePasswordResetCommand
            {
                RawToken = request.Token,
                NewPassword = request.NewPassword
            };

            var response = await passwordResetUseCase
                .CompletePasswordReset(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
       .AllowAnonymous()
       .WithName("CompletePasswordReset")
       .WithTags("PasswordReset");
    }
}

