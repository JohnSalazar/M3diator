namespace M3diator;

/// <summary>
/// Defines the interface for publishing notifications to multiple handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Asynchronously publishes a notification to multiple handlers.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task Publish(object notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously publishes a notification to multiple handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification object.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}