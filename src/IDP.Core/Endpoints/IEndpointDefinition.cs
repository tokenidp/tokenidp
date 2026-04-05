namespace IDP.Core.Endpoints;

internal interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}
