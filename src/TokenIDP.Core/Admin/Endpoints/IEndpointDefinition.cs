namespace TokenIDP.Core.Admin.Endpoints;

public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}

