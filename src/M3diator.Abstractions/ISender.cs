namespace M3diator;

/// <summary>
/// Defines the interface for sending requests to a single handler.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Asynchronously sends a request to a single handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request object.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the send operation. The task result contains the handler's response.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a request to a single handler (abstraction over Send).
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the send operation. The task result contains the handler's response as object?.</returns>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);
}