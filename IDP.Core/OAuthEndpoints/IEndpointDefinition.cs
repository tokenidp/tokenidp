namespace IDP.Core.OAuthEndpoints;

public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}
