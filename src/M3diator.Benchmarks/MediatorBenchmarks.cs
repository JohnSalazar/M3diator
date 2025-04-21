using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using M3diator.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace M3diator.Benchmarks;

public record BenchRequest(int Value) : IRequest<BenchResponse>;
public record BenchResponse(int Value);

public record BenchVoidRequest(string Id) : IRequest;

public record BenchNotification(Guid EventId) : INotification;

public class BenchRequestHandler : IRequestHandler<BenchRequest, BenchResponse>
{
    public async Task<BenchResponse> Handle(BenchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        return new BenchResponse(request.Value * 2);
    }
}

public class BenchVoidRequestHandler : IRequestHandler<BenchVoidRequest>
{
    public async Task<Unit> Handle(BenchVoidRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        if (string.IsNullOrEmpty(request.Id)) await Task.Delay(0);
        return Unit.Value;
    }

    async Task<Unit> IRequestHandler<BenchVoidRequest, Unit>.Handle(BenchVoidRequest request, CancellationToken cancellationToken)
       => await Handle(request, cancellationToken);
}

public class BenchNotificationHandler1 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        if (notification.EventId == Guid.Empty) await Task.Delay(0);
    }
}
public class BenchNotificationHandler2 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        if (notification.EventId == Guid.Empty) await Task.Delay(0);
    }
}
public class BenchNotificationHandler3 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        if (notification.EventId == Guid.Empty) await Task.Delay(0);
    }
}

public class BenchPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        var response = await next().ConfigureAwait(false);
        await Task.Delay(1, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }
}

public class SecondBenchBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        var response = await next().ConfigureAwait(false);
        await Task.Delay(1, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }
}


[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
// [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net90)]
public class MediatorBenchmarks
{
    private IServiceProvider _providerSingleHandlerOneBehavior = null!;
    private IServiceProvider _providerMultipleHandlersOneBehavior = null!;
    private IServiceProvider _providerMultipleHandlersMultipleBehaviors = null!;
    private IServiceProvider _providerNoBehaviors = null!;

    private IMediator _mediatorSingleHandlerOneBehavior = null!;
    private IMediator _mediatorMultipleHandlersOneBehavior = null!;
    private IMediator _mediatorMultipleHandlersMultipleBehaviors = null!;
    private IMediator _mediatorNoBehaviors = null!;

    private readonly BenchRequest _benchRequest = new(10);
    private readonly BenchVoidRequest _benchVoidRequest = new("VoidId");
    private readonly BenchNotification _benchNotification = new(Guid.NewGuid());

    private CancellationTokenSource _cts = null!;
    private CancellationToken _validToken;
    private CancellationToken _cancelledToken;

    [Params(1, 3)]
    public int NumberOfNotificationHandlers;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _cts = new CancellationTokenSource();
        _validToken = _cts.Token;
        var cancelledCts = new CancellationTokenSource();
        cancelledCts.Cancel();
        _cancelledToken = cancelledCts.Token;

        var services1 = new ServiceCollection();
        services1.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            cfg.WithLifetime(ServiceLifetime.Transient);
        });

        services1.AddTransient<IRequestHandler<BenchRequest, BenchResponse>, BenchRequestHandler>();
        services1.AddTransient<IRequestHandler<BenchVoidRequest, Unit>, BenchVoidRequestHandler>();
        services1.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler1>();

        _providerSingleHandlerOneBehavior = services1.BuildServiceProvider();
        _mediatorSingleHandlerOneBehavior = _providerSingleHandlerOneBehavior.GetRequiredService<IMediator>();


        var services3 = new ServiceCollection();
        services3.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            cfg.WithLifetime(ServiceLifetime.Transient);
        });
        services3.AddTransient<IRequestHandler<BenchRequest, BenchResponse>, BenchRequestHandler>();

        services3.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler1>();
        services3.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler2>();
        services3.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler3>();

        _providerMultipleHandlersOneBehavior = services3.BuildServiceProvider();
        _mediatorMultipleHandlersOneBehavior = _providerMultipleHandlersOneBehavior.GetRequiredService<IMediator>();


        var services4 = new ServiceCollection();
        services4.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            cfg.AddOpenBehavior(typeof(SecondBenchBehavior<,>));
            cfg.WithLifetime(ServiceLifetime.Transient);
        });
        services4.AddTransient<IRequestHandler<BenchRequest, BenchResponse>, BenchRequestHandler>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler1>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler2>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler3>();

        _providerMultipleHandlersMultipleBehaviors = services4.BuildServiceProvider();
        _mediatorMultipleHandlersMultipleBehaviors = _providerMultipleHandlersMultipleBehaviors.GetRequiredService<IMediator>();

        var services0 = new ServiceCollection();
        services0.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly);
            cfg.WithLifetime(ServiceLifetime.Transient);
        });
        services0.AddTransient<IRequestHandler<BenchRequest, BenchResponse>, BenchRequestHandler>();
        services0.AddTransient<IRequestHandler<BenchVoidRequest, Unit>, BenchVoidRequestHandler>();
        services0.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler1>();

        _providerNoBehaviors = services0.BuildServiceProvider();
        _mediatorNoBehaviors = _providerNoBehaviors.GetRequiredService<IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cts?.Dispose();
    }

    [Benchmark(Description = "Send<T> No Pipeline")]
    public Task<BenchResponse> Send_NoPipeline()
    {
        return _mediatorNoBehaviors.Send(_benchRequest, _validToken);
    }

    [Benchmark(Description = "Send<T> 1 Behavior (Scan)")]
    public Task<BenchResponse> Send_OnePipelineBehavior()
    {
        return _mediatorSingleHandlerOneBehavior.Send(_benchRequest, _validToken);
    }

    [Benchmark(Description = "Send<T> 2 Behaviors (Scan + Explicit)")]
    public Task<BenchResponse> Send_TwoPipelineBehaviors()
    {
        return _mediatorMultipleHandlersMultipleBehaviors.Send(_benchRequest, _validToken);
    }

    [Benchmark(Description = "Send<void> 1 Behavior (Scan)")]
    public Task SendVoid_OnePipelineBehavior()
    {
        return _mediatorSingleHandlerOneBehavior.Send(_benchVoidRequest, _validToken);
    }

    [Benchmark(Description = "Send(object) 1 Behavior (Scan)")]
    public Task<object?> SendObject_OnePipelineBehavior()
    {
        return _mediatorSingleHandlerOneBehavior.Send((object)_benchRequest, _validToken);
    }

    [Benchmark(Description = "Resolve+Exec Handler Directly")]
    public Task<BenchResponse> Send_DirectHandlerResolution()
    {
        var handler = _providerSingleHandlerOneBehavior.GetRequiredService<IRequestHandler<BenchRequest, BenchResponse>>();
        return handler.Handle(_benchRequest, _validToken);
    }

    [Benchmark(Description = "Send<T> 1 Behavior (Valid Token)")]
    public Task<BenchResponse> SendRequest_WithCancellation_Valid()
    {
        return _mediatorSingleHandlerOneBehavior.Send(_benchRequest, _validToken);
    }

    [Benchmark(Description = "Send<T> 1 Behavior (Cancelled Token)")]
    public async Task SendRequest_WithCancellation_Cancelled()
    {
        try
        {
            await _mediatorSingleHandlerOneBehavior.Send(_benchRequest, _cancelledToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    [Benchmark(Description = "Publish 1 Handler / 1 Behavior")]
    public Task Publish_OneHandler_OneBehavior()
    {
        return _mediatorSingleHandlerOneBehavior.Publish(_benchNotification, _validToken);
    }

    [Benchmark(Description = "Publish 3 Handlers / 1 Behavior")]
    public Task Publish_ThreeHandlers_OneBehavior()
    {
        return _mediatorMultipleHandlersOneBehavior.Publish(_benchNotification, _validToken);
    }

    [Benchmark(Description = "Publish 3 Handlers / 2 Behaviors")]
    public Task Publish_ThreeHandlers_TwoBehaviors()
    {
        return _mediatorMultipleHandlersMultipleBehaviors.Publish(_benchNotification, _validToken);
    }

    [Benchmark(Description = "Publish(object) 3 Handlers / 1 Behavior")]
    public Task PublishObject_ThreeHandlers_OneBehavior()
    {
        return _mediatorMultipleHandlersOneBehavior.Publish((object)_benchNotification, _validToken);
    }

    [Benchmark(Description = "Resolve+Exec 3 Handlers Directly")]
    public Task Publish_DirectHandlerResolution_ThreeHandlers()
    {
        var handlers = _providerMultipleHandlersOneBehavior.GetServices<INotificationHandler<BenchNotification>>();
        return ExecuteHandlersSequentially(handlers, _benchNotification, _validToken);
    }

    [Benchmark(Description = "Publish 3 Handlers (Valid Token)")]
    public Task Publish_WithCancellation_Valid()
    {
        return _mediatorMultipleHandlersOneBehavior.Publish(_benchNotification, _validToken);
    }

    [Benchmark(Description = "Publish 3 Handlers (Cancelled Token)")]
    public async Task Publish_WithCancellation_Cancelled()
    {
        try
        {
            await _mediatorMultipleHandlersOneBehavior.Publish(_benchNotification, _cancelledToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Any(e => e is OperationCanceledException))
        {
        }
    }

    private async Task ExecuteHandlersSequentially(IEnumerable<INotificationHandler<BenchNotification>> handlers, BenchNotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting M3diator Benchmarks...");

        var summary = BenchmarkRunner.Run<MediatorBenchmarks>();

        Console.WriteLine("\n--- Benchmark Summary ---");
        Console.WriteLine(summary);
        Console.WriteLine("--- Benchmarks Finished ---");
    }
}