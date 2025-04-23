using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace M3diator.Benchmarks;

public record BenchRequest(int Value) : IRequest<BenchResponse>;
public record BenchResponse(int Value);
public class BenchRequestHandler : IRequestHandler<BenchRequest, BenchResponse>
{
    public async Task<BenchResponse> Handle(BenchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        return new BenchResponse(request.Value * 2);
    }
}

public record BenchVoidRequest(string Id) : IRequest;
public class BenchVoidRequestHandler : IRequestHandler<BenchVoidRequest>
{
    public async Task<Unit> Handle(BenchVoidRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        if (string.IsNullOrEmpty(request.Id)) await Task.Delay(0, cancellationToken);
        return Unit.Value;
    }

    async Task<Unit> IRequestHandler<BenchVoidRequest, Unit>.Handle(BenchVoidRequest request, CancellationToken cancellationToken)
       => await Handle(request, cancellationToken);
}

public record BenchNotification(Guid EventId) : INotification;
public class BenchNotificationHandler1 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        if (notification.EventId == Guid.Empty) await Task.Delay(0, cancellationToken);
    }
}
public class BenchNotificationHandler2 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        if (notification.EventId == Guid.Empty) await Task.Delay(0, cancellationToken);
    }
}
public class BenchNotificationHandler3 : INotificationHandler<BenchNotification>
{
    public async Task Handle(BenchNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        if (notification.EventId == Guid.Empty) await Task.Delay(0, cancellationToken);
    }
}

public class BenchPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        var response = await next().ConfigureAwait(false);
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }
}
public class SecondBenchBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        var response = await next().ConfigureAwait(false);
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }
}

public record BenchStreamItem(int Value);
public record BenchStreamRequest(int Count) : IStreamRequest<BenchStreamItem>;
public class BenchStreamRequestHandler : IStreamRequestHandler<BenchStreamRequest, BenchStreamItem>
{
    public async IAsyncEnumerable<BenchStreamItem> Handle(
        BenchStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new BenchStreamItem(i);
        }
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MarkdownExporterAttribute.GitHub]
// [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net90)]
public class MediatorBenchmarks
{
    private IServiceProvider _providerSingleHandlerOneBehavior = null!;
    private IServiceProvider _providerMultipleHandlersOneBehavior = null!;
    private IServiceProvider _providerMultipleHandlersMultipleBehaviors = null!;
    private IServiceProvider _providerStreamRequest = null!;
    private IServiceProvider _providerNoBehaviors = null!;

    private IMediator _mediatorSingleHandlerOneBehavior = null!;
    private IMediator _mediatorMultipleHandlersOneBehavior = null!;
    private IMediator _mediatorMultipleHandlersMultipleBehaviors = null!;
    private IMediator _mediatorStreamRequest = null!;
    private IMediator _mediatorNoBehaviors = null!;

    private readonly BenchRequest _benchRequest = new(10);
    private readonly BenchVoidRequest _benchVoidRequest = new("VoidId");
    private readonly BenchNotification _benchNotification = new(Guid.NewGuid());

    private BenchStreamRequest _benchStreamRequest_1 = new(1);
    private BenchStreamRequest _benchStreamRequest_10 = new(10);
    private BenchStreamRequest _benchStreamRequest_100 = new(100);

    private CancellationTokenSource _cts = null!;
    private CancellationToken _validToken;
    private CancellationToken _cancelledToken;

    [Params(1, 3)]
    public int NumberOfNotificationHandlers;

    [Params(1, 100)]
    public int Operations;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] GlobalSetup Started...");

        _cts = new CancellationTokenSource();
        _validToken = _cts.Token;
        var cancelledCts = new CancellationTokenSource();
        cancelledCts.Cancel();
        _cancelledToken = cancelledCts.Token;

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

        var services2 = new ServiceCollection();
        services2.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            cfg.WithLifetime(ServiceLifetime.Transient);
        });
        services2.AddTransient<IStreamRequestHandler<BenchStreamRequest, BenchStreamItem>, BenchStreamRequestHandler>();

        _providerStreamRequest = services2.BuildServiceProvider();
        _mediatorStreamRequest = _providerStreamRequest.GetRequiredService<IMediator>();

        var services3 = new ServiceCollection();
        services3.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly);
            cfg.WithLifetime(ServiceLifetime.Transient);
        });
        services3.AddTransient<IRequestHandler<BenchRequest, BenchResponse>, BenchRequestHandler>();
        services3.AddTransient<IRequestHandler<BenchVoidRequest, Unit>, BenchVoidRequestHandler>();
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
        services4.AddTransient<IRequestHandler<BenchVoidRequest, Unit>, BenchVoidRequestHandler>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler1>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler2>();
        services4.AddTransient<INotificationHandler<BenchNotification>, BenchNotificationHandler3>();

        _providerMultipleHandlersMultipleBehaviors = services4.BuildServiceProvider();
        _mediatorMultipleHandlersMultipleBehaviors = _providerMultipleHandlersMultipleBehaviors.GetRequiredService<IMediator>();

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] GlobalSetup Finished.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cts?.Dispose();
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] GlobalCleanup Finished.");
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

    [Benchmark(Description = "CreateStream (1 Item) - Consume All")]
    public async Task CreateStream_ConsumeAll_1_Item()
    {
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_1, _validToken);
        await foreach (var _ in stream) { }
    }

    [Benchmark(Description = "CreateStream (10 Items) - Consume All")]
    public async Task CreateStream_ConsumeAll_10_Items()
    {
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_10, _validToken);
        await foreach (var _ in stream) { }
    }

    [Benchmark(Description = "CreateStream (100 Items) - Consume All")]
    public async Task CreateStream_ConsumeAll_100_Items()
    {
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_100, _validToken);
        await foreach (var _ in stream) { }
    }

    [Benchmark(Description = "CreateStream (100 Items) - Consume ToList")]
    public async Task CreateStream_ToList_100_Items()
    {
        var list = new List<BenchStreamItem>(100);
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_100, _validToken);
        await foreach (var item in stream)
        {
            list.Add(item);
        }
        if (list.Count != 100) throw new InvalidOperationException("Benchmark error: Incorrect item count.");
    }

    [Benchmark(Description = "CreateStream (100 Items) - Get Enumerator Only")]
    public async Task CreateStream_GetEnumeratorOnly_100_Items()
    {
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_100, _validToken);
        await using (var enumerator = stream.GetAsyncEnumerator(_validToken))
        {
            await Task.CompletedTask;
        }
    }

    [Benchmark(Description = "CreateStream (100 Items) - Cancelled Token")]
    public async Task CreateStream_Cancelled()
    {
        var stream = _mediatorStreamRequest.CreateStream(_benchStreamRequest_100, _cancelledToken);
        try
        {
            await foreach (var _ in stream.WithCancellation(_cancelledToken)) { }
        }
        catch (OperationCanceledException) { }
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
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting M3diator Benchmarks...");

        var summary = BenchmarkRunner.Run<MediatorBenchmarks>();

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] --- Benchmark Summary ---");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] --- Benchmarks Finished ---");
    }
}