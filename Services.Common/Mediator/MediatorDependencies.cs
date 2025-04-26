using Microsoft.Extensions.DependencyInjection;

namespace Services.Common.Mediator;

public static class MediatorRegistrationExtensions
{
    public static void AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddTransient<IMediator, Mediator>();

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IRequestHandler<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                if (iface.GetGenericTypeDefinition() == handlerInterface)
                {
                    services.AddTransient(iface, type);
                }
            }
        }
    }
}
