namespace M3diator;

/// <summary>
/// Represents a request that returns a stream of responses.
/// </summary>
/// <typeparam name="TResponse">The type of the response items in the stream.</typeparam>
public interface IStreamRequest<out TResponse> : IBaseRequest;
