using System.Runtime.CompilerServices;

namespace M3diator.Internal;

/// <summary>
/// Base interface for non-generic access to stream request handlers.
/// </summary>
internal abstract class StreamRequestHandlerBase
{
    /// <summary>
    /// Handles the stream request via the wrapped handler.
    /// </summary>
    /// <param name="request">The stream request object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of response objects.</returns>
    internal abstract IAsyncEnumerable<object?> Handle(object request, CancellationToken cancellationToken);
}

/// <summary>
/// Wraps a specific IStreamRequestHandler implementation.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request.</typeparam>
/// <typeparam name="TResponse">The type of the response items in the stream.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="StreamRequestHandlerWrapperImpl{TRequest, TResponse}"/> class.
/// </remarks>
/// <param name="innerHandler">The actual handler instance resolved from DI.</param>
internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse>(IStreamRequestHandler<TRequest, TResponse> innerHandler) : StreamRequestHandlerBase
    where TRequest : IStreamRequest<TResponse>
{
    private readonly IStreamRequestHandler<TRequest, TResponse> _inner = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));

    /// <summary>
    /// Handles the stream request by resolving and invoking the specific handler.
    /// </summary>
    /// <param name="requestObject">The stream request object (cast to TRequest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of response items (cast to object?).</returns>
    internal override async IAsyncEnumerable<object?> Handle(object requestObject, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = _inner.Handle((TRequest)requestObject, cancellationToken);

        await foreach (var item in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
