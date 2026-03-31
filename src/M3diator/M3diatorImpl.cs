using M3diator.Internal;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace M3diator;

/// <summary>
/// Default implementation of IMediator, ISender and IPublisher.
/// Uses stateless cached wrappers that resolve handlers from DI on each call,
/// correctly respecting configured service lifetimes (Transient, Scoped, Singleton).
/// </summary>
internal sealed class M3diatorImpl : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    // Stateless wrapper caches — safe to be static because wrappers hold no state.
    // They resolve handlers from IServiceProvider on each call.
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapperBase> NotificationWrappers = new();
    private static readonly ConcurrentDictionary<Type, StreamRequestHandlerBase> StreamWrappers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="M3diatorImpl"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    public M3diatorImpl(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = GetOrCreateRequestWrapper(request.GetType(), typeof(TResponse));
        var result = await wrapper.Handle(request, _serviceProvider, cancellationToken).ConfigureAwait(false);

        if (typeof(TResponse) == typeof(Unit))
            return default!;

        return (TResponse)result!;
    }

    /// <inheritdoc />
    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Send<Unit>(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = GetResponseType(requestType);
        var wrapper = GetOrCreateRequestWrapper(requestType, responseType);

        return await wrapper.Handle(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var wrapper = GetOrCreateNotificationWrapper(notification.GetType());
        return wrapper.Handle(notification, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification)
        {
            throw new ArgumentException(
                $"Object '{notification.GetType().FullName}' does not implement INotification.",
                nameof(notification));
        }

        var wrapper = GetOrCreateNotificationWrapper(notification.GetType());
        return wrapper.Handle(notification, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = GetOrCreateStreamWrapper(request.GetType(), typeof(TResponse));

        await foreach (var item in wrapper.Handle(request, _serviceProvider, cancellationToken).ConfigureAwait(false))
        {
            if (item is TResponse responseItem)
            {
                yield return responseItem;
            }
            else if (item is null && (!typeof(TResponse).IsValueType || Nullable.GetUnderlyingType(typeof(TResponse)) != null))
            {
                yield return default!;
            }
            else
            {
                throw new InvalidCastException(
                    $"Stream item of type {item?.GetType().FullName ?? "null"} could not be cast to target type {typeof(TResponse).FullName}.");
            }
        }
    }

    private static RequestHandlerBase GetOrCreateRequestWrapper(Type requestType, Type responseType)
    {
        return RequestWrappers.GetOrAdd(requestType, static (reqType, respType) =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(reqType, respType);
            return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
        }, responseType);
    }

    private static NotificationHandlerWrapperBase GetOrCreateNotificationWrapper(Type notificationType)
    {
        return NotificationWrappers.GetOrAdd(notificationType, static notifType =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notifType);
            return (NotificationHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;
        });
    }

    private static StreamRequestHandlerBase GetOrCreateStreamWrapper(Type requestType, Type responseType)
    {
        return StreamWrappers.GetOrAdd(requestType, static (reqType, respType) =>
        {
            var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(reqType, respType);
            return (StreamRequestHandlerBase)Activator.CreateInstance(wrapperType)!;
        }, responseType);
    }

    private static Type GetResponseType(Type requestType)
    {
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is not null)
            return requestInterface.GetGenericArguments()[0];

        if (typeof(IRequest).IsAssignableFrom(requestType))
            return typeof(Unit);

        throw new ArgumentException(
            $"Object '{requestType.FullName}' does not implement IRequest or IRequest<TResponse>.",
            "request");
    }
}
