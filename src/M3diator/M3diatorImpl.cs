using M3diator.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace M3diator;

/// <summary>
/// Default implementation of IMediator, ISender and IPublisher.
/// Handles request sending, notification publishing, and pipeline behavior execution.
/// Implements caching for handler resolution to improve performance.
/// </summary>
public class M3diatorImpl : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();
    private readonly ConcurrentDictionary<Type, List<NotificationHandlerWrapperBase>> _notificationHandlers = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo> SendInternalMethodCache = new();

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
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendInternal(request, cancellationToken);
    }

    /// <summary>
    /// Sends a request without a response (uses Unit).
    /// </summary>
    /// <param name="request">The request object implementing IRequest (implies IRequest&lt;Unit&gt;).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendInternal<Unit>(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();

        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        Type responseType;
        if (requestInterface == null)
        {
            if (request is IRequest)
            {
                responseType = typeof(Unit);
            }
            else
            {
                throw new ArgumentException($"Object '{requestType.FullName}' does not implement IRequest or IRequest<TResponse>.", nameof(request));
            }
        }
        else
        {
            responseType = requestInterface.GetGenericArguments()[0];
        }

        static MethodInfo GetSendInternalMethod(Type respType) =>
            typeof(M3diatorImpl)
                .GetMethod(nameof(SendInternal), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(respType);

        var sendInternalMethod = SendInternalMethodCache.GetOrAdd(responseType, GetSendInternalMethod);

        var taskResult = sendInternalMethod.Invoke(this, new object[] { request, cancellationToken });

        return HandleTaskResult(taskResult, responseType);
    }

    private static async Task<object?> HandleTaskResult(object? taskObject, Type responseType)
    {
        if (taskObject is null) return null;
        if (taskObject is not Task task) throw new InvalidOperationException("Expected a Task object.");

        await task.ConfigureAwait(false);

        if (responseType == typeof(Unit))
        {
            return Unit.Value;
        }
        else
        {
            try
            {
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty == null)
                {
                    throw new InvalidOperationException($"Task type {task.GetType().FullName} did not contain a 'Result' property for response type {responseType.Name}.");
                }
                return resultProperty.GetValue(task);
            }
            catch (TargetInvocationException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get result from task for response type {responseType.Name}.", ex);
            }
        }
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        return PublishInternal(notification, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification iNotification)
        {
            throw new ArgumentException($"Object '{notification.GetType().FullName}' does not implement INotification.", nameof(notification));
        }
        return PublishInternal(iNotification, cancellationToken);
    }

    private async Task<TResponse> SendInternal<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        var handler = _requestHandlers.GetOrAdd(requestType, static (reqType, sp) =>
        {
            var handlerServiceType = typeof(IRequestHandler<,>).MakeGenericType(reqType, typeof(TResponse));
            var handlerInstance = sp.GetRequiredService(handlerServiceType);
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(reqType, typeof(TResponse));
            var wrapper = (RequestHandlerBase)Activator.CreateInstance(wrapperType, handlerInstance)!;
            return wrapper;
        }, _serviceProvider);

        RequestHandlerDelegate<TResponse> handlerDelegate = async () =>
        {
            object? result = await handler.Handle(request, cancellationToken).ConfigureAwait(false);
            if (typeof(TResponse) == typeof(Unit)) { return default!; }
            return (TResponse)result!;
        };

        var behaviorServiceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var resolvedBehaviors = _serviceProvider.GetServices(behaviorServiceType).Reverse();

        RequestHandlerDelegate<TResponse> pipeline = handlerDelegate;
        foreach (var behaviorObj in resolvedBehaviors)
        {
            if (behaviorObj == null) continue;

            var nextDelegate = pipeline;

            pipeline = () => ((dynamic)behaviorObj).Handle((dynamic)request, nextDelegate, cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }


    private async Task PublishInternal(INotification notification, CancellationToken cancellationToken)
    {
        var notificationType = notification.GetType();
        var handlers = _notificationHandlers.GetOrAdd(notificationType, static (notifType, sp) =>
        {
            var handlerServiceType = typeof(INotificationHandler<>).MakeGenericType(notifType);
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notifType);
            var handlerInstances = sp.GetServices(handlerServiceType);
            var wrappers = new List<NotificationHandlerWrapperBase>();
            foreach (var handlerInstance in handlerInstances)
            {
                if (handlerInstance != null)
                {
                    var wrapper = (NotificationHandlerWrapperBase)Activator.CreateInstance(wrapperType, handlerInstance)!;
                    wrappers.Add(wrapper);
                }
            }
            return wrappers;
        }, _serviceProvider);

        if (!handlers.Any()) { return; }

        List<Exception>? exceptions = null;
        foreach (var handler in handlers)
        {
            try { await handler.Handle(notification, cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) { exceptions ??= new List<Exception>(); exceptions.Add(ex); }
        }

        if (exceptions != null && exceptions.Count > 0)
        {
            throw new AggregateException($"One or more errors occurred while publishing notification '{notificationType.FullName}'", exceptions);
        }
    }
}