using Microsoft.Extensions.DependencyInjection;

namespace M3diator.Internal;

/// <summary>
/// Base class for request handler wrappers. Stateless — resolves handlers from DI on each call.
/// </summary>
internal abstract class RequestHandlerBase
{
    internal abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete stateless wrapper for IRequestHandler&lt;TRequest, TResponse&gt;.
/// Resolves the handler and pipeline behaviors from DI on each invocation, respecting configured lifetimes.
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerBase
    where TRequest : IRequest<TResponse>
{
    internal override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        Func<Task<TResponse>> pipeline = () => handler.Handle(typedRequest, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () => b.Handle(typedRequest, next, cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }
}
