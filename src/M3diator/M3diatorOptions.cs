using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace M3diator;

/// <summary>
/// Configuration options for M3diator service registration.
/// </summary>
public class M3diatorOptions
{
    /// <summary>
    /// Gets the list of assemblies to scan for handlers and behaviors.
    /// </summary>
    public List<Assembly> AssembliesToRegister { get; } = [];

    /// <summary>
    /// Gets the lifetime for registered M3diator services (handlers, behaviors, mediator).
    /// Defaults to Transient.
    /// </summary>
    public ServiceLifetime Lifetime { get; private set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Gets the concrete implementation type for the IMediator interface.
    /// </summary>
    internal Type MediatorImplementationType { get; private set; } = typeof(M3diatorImpl);

    /// <summary>
    /// Gets the list of open generic pipeline behavior types explicitly registered via AddOpenBehavior.
    /// </summary>
    internal List<Type> ExplicitlyRegisteredOpenBehaviors { get; } = [];

    /// <summary>
    /// Gets the list of open generic stream pipeline behavior types explicitly registered via AddOpenStreamBehavior.
    /// </summary>
    internal List<Type> ExplicitlyRegisteredOpenStreamBehaviors { get; } = [];

    /// <summary>
    /// Registers services from the assembly containing the specified marker type.
    /// </summary>
    /// <typeparam name="T">The marker type.</typeparam>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Registers services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!AssembliesToRegister.Contains(assembly))
            AssembliesToRegister.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers services from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies ?? [])
        {
            if (assembly is not null && !AssembliesToRegister.Contains(assembly))
                AssembliesToRegister.Add(assembly);
        }
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all registered services (Mediator, Handlers, Behaviors).
    /// </summary>
    /// <param name="lifetime">The desired service lifetime.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions WithLifetime(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Specifies a custom implementation type for the IMediator interface.
    /// </summary>
    /// <typeparam name="TMediator">The custom mediator implementation type.</typeparam>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions UseMediatorImplementation<TMediator>() where TMediator : IMediator
    {
        MediatorImplementationType = typeof(TMediator);
        Lifetime = ServiceLifetime.Transient;
        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior type to be registered explicitly.
    /// </summary>
    /// <param name="openBehaviorType">The open generic type (e.g., <c>typeof(LoggingBehavior&lt;,&gt;)</c>).</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions AddOpenBehavior(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' must be an open generic type definition.",
                nameof(openBehaviorType));

        bool implementsInterface = openBehaviorType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!implementsInterface)
        {
            bool isInterfaceItself = openBehaviorType.IsInterface
                && openBehaviorType.IsGenericType
                && openBehaviorType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);

            if (!isInterfaceItself)
                throw new ArgumentException(
                    $"Type '{openBehaviorType.FullName}' must implement '{typeof(IPipelineBehavior<,>).FullName}'.",
                    nameof(openBehaviorType));
        }

        if (!ExplicitlyRegisteredOpenBehaviors.Contains(openBehaviorType))
            ExplicitlyRegisteredOpenBehaviors.Add(openBehaviorType);

        return this;
    }

    /// <summary>
    /// Adds an open generic stream pipeline behavior type to be registered explicitly.
    /// </summary>
    /// <param name="openBehaviorType">The open generic type (e.g., <c>typeof(LoggingStreamBehavior&lt;,&gt;)</c>).</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorOptions AddOpenStreamBehavior(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' must be an open generic type definition.",
                nameof(openBehaviorType));

        bool implementsInterface = openBehaviorType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>));

        if (!implementsInterface)
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' must implement '{typeof(IStreamPipelineBehavior<,>).FullName}'.",
                nameof(openBehaviorType));

        if (!ExplicitlyRegisteredOpenStreamBehaviors.Contains(openBehaviorType))
            ExplicitlyRegisteredOpenStreamBehaviors.Add(openBehaviorType);

        return this;
    }
}
