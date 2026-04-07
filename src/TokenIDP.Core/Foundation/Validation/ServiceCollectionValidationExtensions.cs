using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace TokenIDP.Core.Foundation.Validation;

internal static class ServiceCollectionValidationExtensions
{
    public static IServiceCollection AddAssemblyValidators(
        this IServiceCollection services,
        Assembly assembly)
    {
        var validatorRegistrations = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(
                implementationType => implementationType
                    .GetInterfaces()
                    .Where(@interface =>
                        @interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Select(@interface => new
                    {
                        ServiceType = @interface,
                        ImplementationType = implementationType
                    }));

        foreach (var registration in validatorRegistrations)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    registration.ServiceType,
                    registration.ImplementationType));
        }

        return services;
    }
}
