using M3diator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace M3diator.Internal;

/// <summary>
/// Handles scanning assemblies and registering M3diator handlers and behaviors.
/// Encapsulates the registration logic to keep ServiceCollectionExtensions clean (SRP).
/// </summary>
internal sealed class HandlerRegister
{
    private readonly IServiceCollection _services;
    private readonly M3diatorServiceConfiguration _config;

    private static readonly Type RequestHandlerInterface = typeof(IRequestHandler<,>);
    private static readonly Type NotificationHandlerInterface = typeof(INotificationHandler<>);
    private static readonly Type PipelineBehaviorInterface = typeof(IPipelineBehavior<,>);
    private static readonly Type StreamRequestHandlerInterface = typeof(IStreamRequestHandler<,>);

    public HandlerRegister(IServiceCollection services, M3diatorServiceConfiguration config)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Scans assemblies specified in the configuration and registers all found
    /// IRequestHandler, INotificationHandler, and IPipelineBehavior implementations.
    /// Uses a single pass approach and registers open generic pipeline behaviors.
    /// </summary>
    public void RegisterHandlersAndBehaviors()
    {
        var typesToRegister = new List<RegistrationInfo>();
        var openGenericPipelineBehaviorTypes = new List<Type>();

        foreach (var assembly in _config.AssembliesToRegister.Distinct())
        {
            foreach (var typeInfo in assembly.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface))
            {
                ScanType(typeInfo, typesToRegister, openGenericPipelineBehaviorTypes);
            }
        }

        RegisterFoundTypes(typesToRegister);
        RegisterOpenGenericPipelineBehaviors(openGenericPipelineBehaviorTypes);
    }

    /// <summary>
    /// Scans a single type for relevant M3diator interfaces and categorizes it for registration.
    /// </summary>
    private void ScanType(TypeInfo typeInfo, List<RegistrationInfo> typesToRegister, List<Type> openGenericPipelineBehaviors)
    {
        var interfaces = typeInfo.GetInterfaces();

        if (typeInfo.IsGenericTypeDefinition &&
            interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == PipelineBehaviorInterface))
        {
            openGenericPipelineBehaviors.Add(typeInfo.AsType());
            return;
        }

        if (typeInfo.IsGenericTypeDefinition) return;

        foreach (var implementedInterface in interfaces)
        {
            if (!implementedInterface.IsGenericType) continue;

            var genericTypeDef = implementedInterface.GetGenericTypeDefinition();
            var implementationType = typeInfo.AsType();

            if (genericTypeDef == RequestHandlerInterface)
            {
                typesToRegister.Add(new RegistrationInfo(implementedInterface, implementationType));
            }
            else if (genericTypeDef == NotificationHandlerInterface)
            {
                typesToRegister.Add(new RegistrationInfo(implementedInterface, implementationType));
            }
            else if (genericTypeDef == PipelineBehaviorInterface)
            {
                typesToRegister.Add(new RegistrationInfo(implementedInterface, implementationType));
            }
            else if (genericTypeDef == StreamRequestHandlerInterface)
            {
                typesToRegister.Add(new RegistrationInfo(implementedInterface, implementationType));
            }
        }
    }

    /// <summary>
    /// Registers the categorized concrete handlers and closed behaviors.
    /// </summary>
    private void RegisterFoundTypes(List<RegistrationInfo> typesToRegister)
    {
        foreach (var regInfo in typesToRegister)
        {
            var descriptor = new ServiceDescriptor(regInfo.ServiceType, regInfo.ImplementationType, _config.Lifetime);
            _services.Add(descriptor);
        }
    }

    /// <summary>
    /// Registers the found open generic pipeline behaviors (from scanning and explicit configuration).
    /// </summary>
    private void RegisterOpenGenericPipelineBehaviors(List<Type> scannedOpenGenericBehaviors)
    {
        var allOpenBehaviors = scannedOpenGenericBehaviors
            .Concat(_config.ExplicitlyRegisteredOpenBehaviors)
            .Distinct()
            .ToList();

        foreach (var openBehaviorType in allOpenBehaviors)
        {
            _services.TryAddEnumerable(new ServiceDescriptor(PipelineBehaviorInterface, openBehaviorType, _config.Lifetime));
        }
    }

    /// <summary>
    /// Helper record to store registration details during the single scan pass.
    /// </summary>
    private record RegistrationInfo(Type ServiceType, Type ImplementationType);
}