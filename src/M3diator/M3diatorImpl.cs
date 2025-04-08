using M3diator.Internal;
using System.Collections.Concurrent;

namespace M3diator;

/// <summary>
/// The M3diator class is responsible for handling requests and notifications.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="M3diator"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
/// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
public class M3diatorImpl(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> _requestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _notificationHandlers = new();

    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(requestType,
            static t => (RequestHandlerWrapperBase)Activator.CreateInstance(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t, typeof(TResponse)))!);
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when request does not implement IRequest or IRequest&lt;TResponse&gt;.</exception>
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();

        var requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(IsRequestInterface);

        if (requestInterfaceType == null)
        {
            throw new ArgumentException($"{requestType.Name} does not implement IRequest or IRequest<TResponse>.", nameof(request));
        }

        var responseType = requestInterfaceType.IsGenericType ? requestInterfaceType.GetGenericArguments()[0] : typeof(Unit);

        var handler = _requestHandlers.GetOrAdd(requestType,
             t =>
             {
                 var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t, responseType);
                 return (RequestHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;
             });

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Determines whether the specified type is a request interface.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is a request interface; otherwise, <c>false</c>.</returns>
    private static bool IsRequestInterface(Type type) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>)) || type == typeof(IRequest);

    /// <summary>
    /// Publishes a notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when notification is null.</exception>
    /// <exception cref="ArgumentException">Thrown when notification does not implement INotification.</exception>
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification)
        {
            throw new ArgumentException($"{notification.GetType().Name} does not implement INotification", nameof(notification));
        }

        var notificationType = notification.GetType();
        var handler = _notificationHandlers.GetOrAdd(notificationType,
           static t => (NotificationHandlerWrapper)Activator.CreateInstance(typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(t))!);

        return handler.Handle(notification, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        return Publish((object)notification, cancellationToken);
    }
}
