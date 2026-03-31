using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace M3diator.Internal;

/// <summary>
/// Base class for stream request handler wrappers. Stateless — resolves handlers from DI on each call.
/// </summary>
internal abstract class StreamRequestHandlerBase
{
    internal abstract IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete stateless wrapper for IStreamRequestHandler&lt;TRequest, TResponse&gt;.
/// Resolves the handler and stream pipeline behaviors from DI on each invocation, respecting configured lifetimes.
/// </summary>
internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerBase
    where TRequest : IStreamRequest<TResponse>
{
    internal override async IAsyncEnumerable<object?> Handle(
        object request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>();

        StreamHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () => b.Handle(typedRequest, next, cancellationToken);
        }

        await foreach (var item in pipeline().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
