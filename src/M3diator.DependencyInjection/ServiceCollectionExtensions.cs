using M3diator;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for scanning and registering M3diator handlers and behaviors using a configuration action.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers M3diator services, scanning for handlers and behaviors based on the provided configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">An action to configure M3diator services, including assembly scanning.</param>
        /// <returns>The service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown if services or configuration is null.</exception>
        /// <exception cref="ArgumentException">Thrown if no assemblies to scan are specified in the configuration.</exception>
        public static IServiceCollection AddM3diator(this IServiceCollection services, Action<M3diatorServiceConfiguration> configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var serviceConfig = new M3diatorServiceConfiguration();
            configuration(serviceConfig);

            if (serviceConfig.AssembliesToRegister.Count == 0)
            {
                throw new ArgumentException("No assemblies were specified to scan for handlers and behaviors. " +
                                            "Use 'cfg.RegisterServicesFromAssembly()' or related methods in the configuration action.", nameof(configuration));
            }

            services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfig.MediatorImplementationType, serviceConfig.Lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfig.Lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfig.Lifetime));

            var requestHandlerInterface = typeof(IRequestHandler<,>);
            var notificationHandlerInterface = typeof(INotificationHandler<>);
            var pipelineBehaviorInterface = typeof(IPipelineBehavior<,>);
            var voidRequestHandlerMarkerInterface = typeof(IRequestHandler<>);

            var types = serviceConfig.AssembliesToRegister
                .SelectMany(a => a.DefinedTypes)
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .ToList();

            foreach (var typeInfo in types)
            {
                var interfaces = typeInfo.GetInterfaces();

                var implementedRequestHandlerInterfaces = interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestHandlerInterface)
                    .ToList();
                foreach (var implementedInterface in implementedRequestHandlerInterfaces)
                {
                    services.Add(new ServiceDescriptor(implementedInterface, typeInfo, serviceConfig.Lifetime));

                    var responseTypeArgument = implementedInterface.GetGenericArguments()[1];
                    if (responseTypeArgument == typeof(Unit))
                    {
                        var requestTypeArgument = implementedInterface.GetGenericArguments()[0];
                        var specificVoidInterface = voidRequestHandlerMarkerInterface.MakeGenericType(requestTypeArgument);
                        services.TryAdd(new ServiceDescriptor(specificVoidInterface, typeInfo, serviceConfig.Lifetime));
                    }
                }

                var implementedNotificationHandlerInterfaces = interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == notificationHandlerInterface)
                    .ToList();

                foreach (var implementedInterface in implementedNotificationHandlerInterfaces)
                {
                    services.Add(new ServiceDescriptor(implementedInterface, typeInfo, serviceConfig.Lifetime));
                }

                var implementedPipelineBehaviorInterfaces = interfaces
                   .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == pipelineBehaviorInterface)
                   .ToList();

                foreach (var implementedInterface in implementedPipelineBehaviorInterfaces)
                {
                    services.Add(new ServiceDescriptor(implementedInterface, typeInfo, serviceConfig.Lifetime));
                }
            }

            return services;
        }
    }
}
