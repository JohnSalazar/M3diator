using M3diator.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace M3diator.Tests
{
    public record Ping(string Message) : IRequest<Pong>;
    public record Pong(string Message);
    public record VoidRequest() : IRequest;
    public record SpyRequest : IRequest<SpyResponse>;
    public record SpyResponse;
    public record SpyNotification : INotification;


    public class PingHandler : IRequestHandler<Ping, Pong> { public Task<Pong> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(new Pong($"Received: {request.Message}")); }
    public class VoidRequestHandler : IRequestHandler<VoidRequest> { private readonly List<string>? _log; public const string CalledMarker = "VoidRequestHandler CALLED"; public VoidRequestHandler(List<string>? executionLog = null) { _log = executionLog; } public Task Handle(VoidRequest request, CancellationToken cancellationToken) { _log?.Add(CalledMarker); Console.WriteLine(CalledMarker); return Task.CompletedTask; } Task<Unit> IRequestHandler<VoidRequest, Unit>.Handle(VoidRequest request, CancellationToken cancellationToken) { _log?.Add(CalledMarker + " (Unit)"); Console.WriteLine(CalledMarker + " (Unit)"); return Unit.Task; } }
    public class SpyRequestHandler(List<string>? executionLog = null) : IRequestHandler<SpyRequest, SpyResponse> { private readonly List<string>? _log = executionLog; public const string CalledMarker = "SpyRequestHandler.Handle CALLED"; public Task<SpyResponse> Handle(SpyRequest request, CancellationToken cancellationToken) { _log?.Add(CalledMarker); Console.WriteLine(CalledMarker); return Task.FromResult(new SpyResponse()); } }
    public class SpyNotificationHandler(List<string>? executionLog = null) : INotificationHandler<SpyNotification> { private readonly List<string>? _log = executionLog; public const string CalledMarker = "SpyNotificationHandler.Handle CALLED"; public Task Handle(SpyNotification notification, CancellationToken cancellationToken) { _log?.Add(CalledMarker); Console.WriteLine(CalledMarker); return Task.CompletedTask; } }
    public class SpyNotificationHandler2(List<string>? executionLog = null) : INotificationHandler<SpyNotification> { private readonly List<string>? _log = executionLog; public const string CalledMarker = "SpyNotificationHandler2.Handle CALLED"; public Task Handle(SpyNotification notification, CancellationToken cancellationToken) { _log?.Add(CalledMarker); Console.WriteLine(CalledMarker); return Task.CompletedTask; } }
    public class FailingSpyNotificationHandler(List<string>? executionLog = null) : INotificationHandler<SpyNotification> { private readonly List<string>? _log = executionLog; public const string CalledMarker = "FailingSpyNotificationHandler.Handle CALLED"; public const string FailureMessage = "Handler Failed Deliberately"; public Task Handle(SpyNotification notification, CancellationToken cancellationToken) { _log?.Add(CalledMarker); Console.WriteLine(CalledMarker); throw new InvalidOperationException(FailureMessage); } }


    public class PassThroughBehavior<TRequest, TResponse>(List<string>? executionLog = null) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> { private readonly List<string>? _log = executionLog; public string Id { get; set; } = Guid.NewGuid().ToString("N")[..6]; public string Marker => $"PassThroughBehavior ({Id}) Executed"; public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) { _log?.Add(Marker); Console.WriteLine(Marker); return next(); } }
    public class FailingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> { public const string CalledMarker = "FailingBehavior Executed - THROWING"; public const string ExceptionMessage = "Simulated pipeline failure"; public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) { Console.WriteLine(CalledMarker); throw new InvalidOperationException(ExceptionMessage); } }
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> { public const string MarkerBefore = "LoggingBehavior - Before"; public const string MarkerAfter = "LoggingBehavior - After"; public const string MarkerException = "LoggingBehavior - Exception"; private readonly List<string>? _logList; private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger; public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>>? logger = null, List<string>? executionLog = null) { _logger = logger ?? NullLogger<LoggingBehavior<TRequest, TResponse>>.Instance; _logList = executionLog; } public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) { string requestTypeName = typeof(TRequest).Name; string beforeMessage = $"{MarkerBefore} {requestTypeName}"; _logList?.Add(beforeMessage); _logger.LogInformation("Handling request {RequestType}", requestTypeName); try { var response = await next().ConfigureAwait(false); string responseTypeName = typeof(TResponse).Name; string afterMessage = $"{MarkerAfter} {requestTypeName} -> {responseTypeName}"; _logList?.Add(afterMessage); _logger.LogInformation("Finished handling {RequestType}, returning {ResponseType}", requestTypeName, responseTypeName); return response; } catch (Exception ex) { string exceptionMessage = $"{MarkerException} {requestTypeName}: {ex.Message}"; _logList?.Add(exceptionMessage); _logger.LogError(ex, "Exception handling {RequestType}", requestTypeName); throw; } } }

}

namespace M3diator.Tests.TestAssemblyMarker
{
    using M3diator.Internal;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public record TestRequest() : IRequest<TestResponse>; public record TestResponse(); public record TestNotification() : INotification;
    public class TestRequestHandler : IRequestHandler<TestRequest, TestResponse> { public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken) => Task.FromResult(new TestResponse()); }
    public class TestNotificationHandler : INotificationHandler<TestNotification> { public Task Handle(TestNotification notification, CancellationToken cancellationToken) => Task.CompletedTask; }
    public class TestNotificationHandler2 : INotificationHandler<TestNotification> { public Task Handle(TestNotification notification, CancellationToken cancellationToken) => Task.CompletedTask; }
    public class TestPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> { public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) { Console.WriteLine("TestPipelineBehavior Executed"); return next(); } }
    public class MarkerType { }
    public class MyCustomMediatorForTest : IMediator { public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => Task.FromResult(default(TResponse)!); public Task Send(IRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null); public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask; public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask; }

}