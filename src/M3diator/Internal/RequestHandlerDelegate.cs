namespace M3diator.Internal;

using System.Threading.Tasks;

/// <summary>
/// Represents an async continuation for the next task to execute in the pipeline
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Awaitable task containing the <typeparamref name="TResponse"/></returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();