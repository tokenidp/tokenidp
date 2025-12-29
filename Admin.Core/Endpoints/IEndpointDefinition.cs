namespace Admin.Core.Endpoints;

public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}
