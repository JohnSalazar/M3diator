using Microsoft.Extensions.DependencyInjection;

namespace M3diator.Internal;

/// <summary>
/// Base class for notification handler wrappers. Stateless — resolves handlers from DI on each call.
/// </summary>
internal abstract class NotificationHandlerWrapperBase
{
    internal abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete stateless wrapper for INotificationHandler&lt;TNotification&gt;.
/// Resolves all notification handlers from DI on each invocation, respecting configured lifetimes.
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapperBase
    where TNotification : INotification
{
    internal override async Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more errors occurred while publishing notification '{typeof(TNotification).FullName}'",
                exceptions);
        }
    }
}
