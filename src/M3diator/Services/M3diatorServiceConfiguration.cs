using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace M3diator.Services;

/// <summary>
/// Configuration options for M3diator service registration.
/// </summary>
public class M3diatorServiceConfiguration
{
    /// <summary>
    /// Gets the list of assemblies to scan for handlers and behaviors.
    /// </summary>
    public List<Assembly> AssembliesToRegister { get; } = new List<Assembly>();

    /// <summary>
    /// Gets the lifetime for registered M3diator services (handlers, behaviors, mediator).
    /// Defaults to Transient.
    /// </summary>
    public ServiceLifetime Lifetime { get; private set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Gets the concrete implementation type for the IMediator interface.
    /// Defaults to M3diatorImpl.
    /// </summary>
    public Type MediatorImplementationType { get; private set; } = typeof(M3diatorImpl);

    /// <summary>
    /// Gets the list of open generic pipeline behavior types explicitly registered via AddOpenBehavior.
    /// Marked internal as it's intended for use by the HandlerRegistrar.
    /// </summary>
    internal List<Type> ExplicitlyRegisteredOpenBehaviors { get; } = new List<Type>();

    /// <summary>
    /// Registers services from the assembly containing the specified marker type.
    /// </summary>
    /// <typeparam name="T">The marker type.</typeparam>
    /// <returns>The configuration object for chaining.</returns>
    public M3diatorServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
    {
        return RegisterServicesFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The configuration object for chaining.</returns>
    public M3diatorServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!AssembliesToRegister.Contains(assembly))
        {
            AssembliesToRegister.Add(assembly);
        }
        return this;
    }

    /// <summary>
    /// Registers services from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The configuration object for chaining.</returns>
    public M3diatorServiceConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies ?? Enumerable.Empty<Assembly>())
        {
            if (assembly != null && !AssembliesToRegister.Contains(assembly))
            {
                AssembliesToRegister.Add(assembly);
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all registered services (Mediator, Handlers, Behaviors).
    /// </summary>
    /// <param name="lifetime">The desired service lifetime.</param>
    /// <returns>The configuration object for chaining.</returns>
    public M3diatorServiceConfiguration WithLifetime(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Specifies a custom implementation type for the IMediator interface.
    /// </summary>
    /// <typeparam name="TMediator">The custom mediator implementation type.</typeparam>
    /// <returns>The configuration object for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if TMediator does not implement IMediator.</exception>
    public M3diatorServiceConfiguration UseMediatorImplementation<TMediator>() where TMediator : IMediator
    {
        MediatorImplementationType = typeof(TMediator);
        Lifetime = ServiceLifetime.Transient;
        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior type to be registered explicitly.
    /// These behaviors are registered in the order they are added, after any behaviors found during assembly scanning.
    /// </summary>
    /// <param name="openBehaviorType">
    /// The open generic type of the pipeline behavior (e.g., <c>typeof(LoggingBehavior&lt;,&gt;)</c>).
    /// </param>
    /// <returns>The configuration object for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if openBehaviorType is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the type is not an open generic definition or doesn't implement <c>IPipelineBehavior&lt;,&gt;</c>.
    /// </exception>
    public M3diatorServiceConfiguration AddOpenBehavior(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"Type '{openBehaviorType.FullName}' must be an open generic type definition.", nameof(openBehaviorType));
        }

        bool implementsInterface = openBehaviorType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!implementsInterface)
        {
            bool isInterfaceItself = openBehaviorType.IsInterface && openBehaviorType.IsGenericType && openBehaviorType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
            if (!isInterfaceItself)
            {
                throw new ArgumentException($"Type '{openBehaviorType.FullName}' must implement '{typeof(IPipelineBehavior<,>).FullName}'.", nameof(openBehaviorType));
            }
        }

        if (!ExplicitlyRegisteredOpenBehaviors.Contains(openBehaviorType))
        {
            ExplicitlyRegisteredOpenBehaviors.Add(openBehaviorType);
        }

        return this;
    }
}