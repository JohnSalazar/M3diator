using M3diator.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for scanning and registering M3diator handlers and behaviors using a configuration action.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers M3diator services, scanning for handlers and behaviors based on the provided configuration.
    /// This optimized version scans assemblies once and registers open generic behaviors directly.
    /// It delegates the scanning and registration logic to an internal HandlerRegistrar class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">An action to configure M3diator services, including assembly scanning.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if services or configuration is null.</exception>
    /// <exception cref="ArgumentException">Thrown if no assemblies to scan are specified in the configuration,
    /// or if the MediatorImplementationType is not assignable to IMediator.</exception>
    public static IServiceCollection AddM3diator(this IServiceCollection services, Action<M3diatorServiceConfigurationOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new M3diatorServiceConfigurationOptions();
        configuration(options);

        var serviceConfig = options.InternalConfig;

        if (serviceConfig.AssembliesToRegister.Count == 0)
        {
            throw new ArgumentException("No assemblies were specified to scan for handlers and behaviors. " +
                                        "Use 'cfg.RegisterServicesFromAssembly()' or related methods in the configuration action.", nameof(configuration));
        }

        if (!typeof(IMediator).IsAssignableFrom(serviceConfig.MediatorImplementationType))
        {
            throw new ArgumentException($"The specified MediatorImplementationType '{serviceConfig.MediatorImplementationType.FullName}' " +
                                        $"does not implement '{nameof(IMediator)}'.", nameof(configuration));
        }

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfig.MediatorImplementationType, serviceConfig.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfig.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfig.Lifetime));

        var registrar = new HandlerRegister(services, serviceConfig);
        registrar.RegisterHandlersAndBehaviors();

        return services;
    }
}