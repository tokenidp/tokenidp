using IDP.Core.UseCases;

namespace IDP.Core.Endpoints;

public class DeviceFlowEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/device_authorization");

        authGroup.MapPost("", static async (DeviceAuthorizationRequest request,
            DeviceAuthorizationUseCase useCase) =>
        {      
            var result = await useCase.CreateAsync(request, CancellationToken.None);

            return result;
        })
        .WithName("DeviceAuthorization")
        .WithTags("DeviceAuthorization");
    }
}
