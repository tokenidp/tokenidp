using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class BackchannelAuthenticationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/backchannel-authentication");

        authGroup.MapPost("", static async (
            HttpContext httpContext,
            BackchannelAuthenticationEndpointClientAuthService clientAuthService,
            CibaBackchannelAuthenticationUseCase useCase) =>
        {
            try
            {
                var request = await clientAuthService.BuildValidatedRequestAsync(httpContext);
                var result = await useCase.CreateAsync(request, httpContext.RequestAborted);
                return Results.Ok(ApiResult<CibaBackchannelAuthenticationResponse>.Success(result));
            }
            catch (BackchannelAuthenticationValidationException ex)
            {
                return BackchannelAuthenticationValidationResultFactory.Create(ex);
            }
        })
        .WithName("BackchannelAuthentication")
        .WithTags("BackchannelAuthentication");
    }
}
