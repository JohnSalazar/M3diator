using M3diator;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configuration options for registering M3diator services.
    /// </summary>
    public class M3diatorServiceConfiguration
    {
        /// <summary>
        /// Gets the list of assemblies to scan for handlers and behaviors.
        /// Populated via the RegisterServicesFrom... methods.
        /// </summary>
        internal List<Assembly> AssembliesToRegister { get; } = new List<Assembly>();

        /// <summary>
        /// Gets or sets the concrete implementation type for <see cref="IMediator"/>.
        /// Default: <see cref="M3diatorImpl"/>.
        /// </summary>
        public Type MediatorImplementationType { get; set; } = typeof(M3diatorImpl);

        /// <summary>
        /// Gets or sets the service lifetime for the Mediator, Handlers, and Pipeline Behaviors.
        /// Default: Transient.
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

        /// <summary>
        /// Adds the assembly containing the specified marker type to the list of assemblies to scan.
        /// </summary>
        /// <typeparam name="T">The marker type.</typeparam>
        /// <returns>The current configuration instance for chaining.</returns>
        public M3diatorServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        {
            return RegisterServicesFromAssembly(typeof(T).Assembly);
        }

        /// <summary>
        /// Adds the specified assembly to the list of assemblies to scan.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The current configuration instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if assembly is null.</exception>
        public M3diatorServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            if (!AssembliesToRegister.Contains(assembly))
            {
                AssembliesToRegister.Add(assembly);
            }
            return this;
        }

        /// <summary>
        /// Adds the specified assemblies to the list of assemblies to scan.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>The current configuration instance for chaining.</returns>
        public M3diatorServiceConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return this;
            foreach (var assembly in assemblies.Distinct())
            {
                if (assembly != null && !AssembliesToRegister.Contains(assembly))
                {
                    AssembliesToRegister.Add(assembly);
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the service lifetime for handlers and behaviors.
        /// </summary>
        /// <param name="lifetime">The desired service lifetime.</param>
        /// <returns>The current configuration instance for chaining.</returns>
        public M3diatorServiceConfiguration WithLifetime(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            return this;
        }

        /// <summary>
        /// Sets the concrete type to use for the IM3diator implementation.
        /// </summary>
        /// <typeparam name="TImplementation">The concrete mediator class type.</typeparam>
        /// <returns>The current configuration instance for chaining.</returns>
        public M3diatorServiceConfiguration UseMediatorImplementation<TImplementation>() where TImplementation : IMediator
        {
            if (!typeof(IMediator).IsAssignableFrom(typeof(TImplementation)))
            {
                throw new InvalidOperationException($"{typeof(TImplementation).Name} must implement IMediator.");
            }

            MediatorImplementationType = typeof(TImplementation);
            return this;
        }
    }
}
