namespace M3diator;

/// <summary>
/// Defines a handler for a stream request.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request.</typeparam>
/// <typeparam name="TResponse">The type of the response items in the stream.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles a stream request asynchronously.
    /// </summary>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of response items.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
