namespace M3diator;

/// <summary>
/// Represents an async continuation for the next stream handler in the pipeline.
/// </summary>
/// <typeparam name="TResponse">Response item type</typeparam>
/// <returns>An async enumerable stream of response items</returns>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();

/// <summary>
/// Pipeline behavior for stream requests. Implement this interface to decorate stream handlers.
/// </summary>
/// <typeparam name="TRequest">Stream request type</typeparam>
/// <typeparam name="TResponse">Response item type</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Pipeline handler for stream requests. Perform any additional behavior and invoke the <paramref name="next"/> delegate as necessary.
    /// </summary>
    /// <param name="request">Incoming stream request</param>
    /// <param name="next">Delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable stream of response items</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
