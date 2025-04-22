namespace M3diator.Internal;

/// <summary>
/// Base class for request handler wrappers. Used for non-generic access and caching.
/// </summary>
internal abstract class RequestHandlerBase
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request object (must be castable to the specific TRequest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the response object (must be castable to the specific TResponse).</returns>
    internal abstract Task<object?> Handle(object request, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete wrapper for IRequestHandler&lt;TRequest, TResponse&gt;.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="RequestHandlerWrapperImpl{TRequest, TResponse}"/> class.
/// </remarks>
/// <param name="innerHandler">The actual handler instance resolved from DI.</param>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> innerHandler) : RequestHandlerBase
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _inner = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));

    /// <summary>
    /// Handles the specific request by casting the input object and calling the inner handler.
    /// </summary>
    /// <param name="requestObject">The request object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the response object.</returns>
    internal override async Task<object?> Handle(object requestObject, CancellationToken cancellationToken)
    {
        var result = await _inner.Handle((TRequest)requestObject, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
