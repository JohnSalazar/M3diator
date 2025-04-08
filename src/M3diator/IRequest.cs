namespace M3diator;

/// <summary>
/// Marker interface to represent a request with a void response
/// </summary>
public interface IRequest : IRequest<Unit>, IBaseRequest;

/// <summary>
/// Marker interface to represent a request with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse> : IBaseRequest;

/// <summary>
/// Base interface for all requests - used simply for variance for Send method
/// </summary>
public interface IBaseRequest { }