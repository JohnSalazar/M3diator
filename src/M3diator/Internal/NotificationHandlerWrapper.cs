namespace M3diator.Internal;

/// <summary>
/// Base class for notification handler wrappers. Used for non-generic access and caching.
/// </summary>
internal abstract class NotificationHandlerWrapperBase
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification object (must be castable to the specific TNotification).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal abstract Task Handle(object notification, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete wrapper for INotificationHandler&lt;TNotification&gt;.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapperBase
  where TNotification : INotification
{
    private readonly INotificationHandler<TNotification> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHandlerWrapperImpl{TNotification}"/> class.
    /// </summary>
    /// <param name="innerHandler">The actual handler instance resolved from DI.</param>
    public NotificationHandlerWrapperImpl(INotificationHandler<TNotification> innerHandler)
    {
        _inner = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
    }

    /// <summary>
    /// Handles the specific notification by casting the input object and calling the inner handler.
    /// </summary>
    /// <param name="notificationObject">The notification object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal override async Task Handle(object notificationObject, CancellationToken cancellationToken)
    {
        await _inner.Handle((TNotification)notificationObject, cancellationToken).ConfigureAwait(false);
    }
}
