using Microsoft.Extensions.DependencyInjection;

namespace M3diator.Internal;

/// <summary>
/// Abstract base class used to wrap a request handler regardless of its specific request/response types.
/// Enables non-generic handling internally.
/// </summary>
internal abstract class RequestHandlerWrapperBase
{
    /// <summary>
    /// Handles a request represented as an object.
    /// </summary>
    /// <param name="request">The request object to handle.</param>
    /// <param name="serviceProvider">The service provider to resolve handler and pipeline dependencies.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, containing the handler's response as an object.</returns>
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Abstract base class for wrapping request handlers with a specific response type.
/// </summary>
/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapperBase
{
    /// <summary>
    /// Handles a request with a specific response type.
    /// </summary>
    /// <param name="request">The typed request object to handle.</param>
    /// <param name="serviceProvider">The service provider to resolve handler and pipeline dependencies.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, containing the typed response.</returns>
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Handles a request represented as an object by casting it and calling the typed Handle method.
    /// </summary>
    /// <param name="request">The request object to handle.</param>
    /// <param name="serviceProvider">The service provider to resolve handler and pipeline dependencies.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, containing the handler's response as an object.</returns>
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Concrete wrapper implementation for invoking a specific request handler (<see cref="IRequestHandler{TRequest, TResponse}"/>)
/// and managing the pipeline behaviors (<see cref="IPipelineBehavior{TRequest, TResponse}"/>).
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
internal class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specific request by resolving the appropriate handler and executing the pipeline.
    /// </summary>
    /// <param name="request">The typed request object.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the response.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the required request handler cannot be resolved from the service provider.</exception>
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();

            if (handler == null)
            {
                string expectedInterface = typeof(TResponse) == typeof(Unit)
                    ? $"IRequestHandler<{typeof(TRequest).Name}> or IRequestHandler<{typeof(TRequest).Name}, Unit>"
                    : $"IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>";

                throw new InvalidOperationException(
                    $"Handler for request type {typeof(TRequest).FullName} could not be resolved. " +
                    $"Ensure that the handler implementing '{expectedInterface}' is registered in the dependency injection container. " +
                    $"Verify that the handler's assembly was scanned by AddM3diator.");
            }

            return handler.Handle((TRequest)request, cancellationToken);
        };

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

        var pipeline = behaviors
            .Aggregate(handlerDelegate, (next, behavior) => () => behavior.Handle((TRequest)request, next, cancellationToken));

        return pipeline();
    }
}

/// <summary>
/// Abstract base class used to wrap a notification handler regardless of its specific notification type.
/// Enables non-generic publishing internally.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    /// <summary>
    /// Handles a notification represented as an object.
    /// </summary>
    /// <param name="notification">The notification object to handle.</param>
    /// <param name="serviceProvider">The service provider to resolve handlers.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation of handling the notification across all relevant handlers.</returns>
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete wrapper implementation for invoking all registered notification handlers (<see cref="INotificationHandler{TNotification}"/>)
/// for a specific notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled.</typeparam>
internal class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    /// <summary>
    /// Handles the specific notification by resolving all registered handlers and invoking them concurrently.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when all handlers have finished processing the notification.</returns>
    public override Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            tasks.Add(Task.Run(() => handler.Handle((TNotification)notification, cancellationToken), cancellationToken));
        }

        return Task.WhenAll(tasks);
    }
}