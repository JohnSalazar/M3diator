using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace M3diator.Internal;

/// <summary>
/// Handles scanning assemblies and registering M3diator handlers and behaviors.
/// </summary>
internal sealed class HandlerRegister(IServiceCollection services, M3diatorOptions config)
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly M3diatorOptions _config = config ?? throw new ArgumentNullException(nameof(config));

    private static readonly Type RequestHandlerInterface = typeof(IRequestHandler<,>);
    private static readonly Type NotificationHandlerInterface = typeof(INotificationHandler<>);
    private static readonly Type PipelineBehaviorInterface = typeof(IPipelineBehavior<,>);
    private static readonly Type StreamRequestHandlerInterface = typeof(IStreamRequestHandler<,>);
    private static readonly Type StreamPipelineBehaviorInterface = typeof(IStreamPipelineBehavior<,>);

    private static readonly HashSet<Type> KnownInterfaces =
    [
        RequestHandlerInterface,
        NotificationHandlerInterface,
        PipelineBehaviorInterface,
        StreamRequestHandlerInterface,
        StreamPipelineBehaviorInterface
    ];

    /// <summary>
    /// Scans assemblies specified in the configuration and registers all found handlers and behaviors.
    /// </summary>
    public void RegisterHandlersAndBehaviors()
    {
        var typesToRegister = new List<RegistrationInfo>();
        var openGenericBehaviors = new List<Type>();
        var openGenericStreamBehaviors = new List<Type>();

        foreach (var assembly in _config.AssembliesToRegister.Distinct())
        {
            foreach (var typeInfo in assembly.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface))
            {
                ScanType(typeInfo, typesToRegister, openGenericBehaviors, openGenericStreamBehaviors);
            }
        }

        RegisterFoundTypes(typesToRegister);
        RegisterOpenGenericBehaviors(PipelineBehaviorInterface, openGenericBehaviors, _config.ExplicitlyRegisteredOpenBehaviors);
        RegisterOpenGenericBehaviors(StreamPipelineBehaviorInterface, openGenericStreamBehaviors, _config.ExplicitlyRegisteredOpenStreamBehaviors);
    }

    private static void ScanType(
        TypeInfo typeInfo,
        List<RegistrationInfo> typesToRegister,
        List<Type> openGenericBehaviors,
        List<Type> openGenericStreamBehaviors)
    {
        var interfaces = typeInfo.GetInterfaces();

        if (typeInfo.IsGenericTypeDefinition)
        {
            if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == PipelineBehaviorInterface))
                openGenericBehaviors.Add(typeInfo.AsType());

            if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == StreamPipelineBehaviorInterface))
                openGenericStreamBehaviors.Add(typeInfo.AsType());

            return;
        }

        foreach (var implementedInterface in interfaces)
        {
            if (!implementedInterface.IsGenericType) continue;

            var genericTypeDef = implementedInterface.GetGenericTypeDefinition();

            if (KnownInterfaces.Contains(genericTypeDef))
            {
                typesToRegister.Add(new RegistrationInfo(implementedInterface, typeInfo.AsType()));
            }
        }
    }

    private void RegisterFoundTypes(List<RegistrationInfo> typesToRegister)
    {
        foreach (var regInfo in typesToRegister)
        {
            _services.Add(new ServiceDescriptor(regInfo.ServiceType, regInfo.ImplementationType, _config.Lifetime));
        }
    }

    private void RegisterOpenGenericBehaviors(Type interfaceType, List<Type> scannedBehaviors, List<Type> explicitBehaviors)
    {
        var allBehaviors = scannedBehaviors
            .Concat(explicitBehaviors)
            .Distinct();

        foreach (var behaviorType in allBehaviors)
        {
            _services.TryAddEnumerable(new ServiceDescriptor(interfaceType, behaviorType, _config.Lifetime));
        }
    }

    private record RegistrationInfo(Type ServiceType, Type ImplementationType);
}
