using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyCal.Domain.Abstractions;

namespace MyCal.Application;

/// <summary>
/// Lets the Application project register its own application services behind one clean method.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all application layer services.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly; // current assembly

        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddHandlersFromAssembly(applicationAssembly);

        return services;
    }

    /// <summary>
    /// Evaluates all concrete types implementing <see cref="ICommandHandler{TCommand,TResult}"/>
    /// or <see cref="IQueryHandler{TQuery,TResult}"/> and registers them as scoped services.
    /// Interfaces and abstract types are ignored.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assembly"></param>
    private static void AddHandlersFromAssembly(
        this IServiceCollection services,
        System.Reflection.Assembly assembly)
    {
        var handlerInterfaceTypes = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        var handlerTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (var handlerType in handlerTypes)
        {
            var serviceTypes = handlerType
                .GetInterfaces()
                .Where(type =>
                    type.IsGenericType &&
                    handlerInterfaceTypes.Contains(type.GetGenericTypeDefinition()));

            foreach (var serviceType in serviceTypes)
            {
                services.AddScoped(serviceType, handlerType);
            }
        }
    }
}
