using M3diator.Services;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides options for configuring M3diator services during registration.
/// This class acts as a public facade over the internal M3diatorServiceConfiguration.
/// </summary>
public class M3diatorServiceConfigurationOptions
{
    /// <summary>
    /// Holds the internal configuration state. Accessible within the assembly.
    /// </summary>
    internal M3diatorServiceConfiguration InternalConfig { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="M3diatorServiceConfigurationOptions"/> class.
    /// </summary>
    public M3diatorServiceConfigurationOptions()
    {
        InternalConfig = new M3diatorServiceConfiguration();
    }

    /// <summary>
    /// Registers services from the assembly containing the specified marker type.
    /// </summary>
    /// <typeparam name="T">The marker type.</typeparam>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions RegisterServicesFromAssemblyContaining<T>()
    {
        InternalConfig.RegisterServicesFromAssemblyContaining<T>();
        return this;
    }

    /// <summary>
    /// Registers services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        InternalConfig.RegisterServicesFromAssembly(assembly);
        return this;
    }

    /// <summary>
    /// Registers services from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        InternalConfig.RegisterServicesFromAssemblies(assemblies);
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all registered services (Mediator, Handlers, Behaviors).
    /// Defaults to Transient.
    /// </summary>
    /// <param name="lifetime">The desired service lifetime.</param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions WithLifetime(ServiceLifetime lifetime)
    {
        InternalConfig.WithLifetime(lifetime);
        return this;
    }

    /// <summary>
    /// Specifies a custom implementation type for the IMediator interface.
    /// </summary>
    /// <typeparam name="TMediator">The custom mediator implementation type, must implement IMediator.</typeparam>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions UseMediatorImplementation<TMediator>() where TMediator : IMediator
    {
        InternalConfig.UseMediatorImplementation<TMediator>();
        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior type to be registered explicitly.
    /// These behaviors are registered in the order they are added.
    /// </summary>
    /// <param name="openBehaviorType">
    /// The open generic type of the pipeline behavior (e.g., <c>typeof(LoggingBehavior&lt;,&gt;)</c>).
    /// Must implement <c>IPipelineBehavior&lt;,&gt;</c>.
    /// </param>
    /// <returns>The options object for chaining.</returns>
    public M3diatorServiceConfigurationOptions AddOpenBehavior(Type openBehaviorType)
    {
        InternalConfig.AddOpenBehavior(openBehaviorType);
        return this;
    }
}