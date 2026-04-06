namespace TokenIDP.Core.OAuth.Endpoints;

internal interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}

