namespace TokenIDP.Core.Admin.Endpoints;

public static class EndpointDefinitionExtensions
{
    public static void RegisterAdminEndpoints(this WebApplication app)
    {
        var definitions = typeof(IEndpointDefinition)
            .Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpointDefinition)) && t.IsClass)
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>();

        foreach (var def in definitions)
        {
            def.RegisterEndpoints(app);
        }
    }
}


