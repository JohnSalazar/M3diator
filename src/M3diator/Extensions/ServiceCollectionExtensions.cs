using M3diator;
using M3diator.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for scanning and registering M3diator handlers and behaviors.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers M3diator services, scanning for handlers and behaviors based on the provided configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">An action to configure M3diator services, including assembly scanning.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddM3diator(this IServiceCollection services, Action<M3diatorOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new M3diatorOptions();
        configuration(options);

        if (options.AssembliesToRegister.Count == 0)
        {
            throw new ArgumentException(
                "No assemblies were specified to scan for handlers and behaviors. " +
                "Use 'cfg.RegisterServicesFromAssembly()' or related methods in the configuration action.",
                nameof(configuration));
        }

        if (!typeof(IMediator).IsAssignableFrom(options.MediatorImplementationType))
        {
            throw new ArgumentException(
                $"The specified MediatorImplementationType '{options.MediatorImplementationType.FullName}' " +
                $"does not implement '{nameof(IMediator)}'.",
                nameof(configuration));
        }

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), options.MediatorImplementationType, options.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), options.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), options.Lifetime));

        var registrar = new HandlerRegister(services, options);
        registrar.RegisterHandlersAndBehaviors();

        return services;
    }
}
