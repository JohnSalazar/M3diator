﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace M3diator.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Send_WithRegisteredHandlerAndExplicitBehavior_ShouldExecuteBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddLogging();
        services.AddSingleton(executionLog);

        services.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Unit).Assembly);
            cfg.AddOpenBehavior(typeof(PassThroughBehavior<,>));
            cfg.WithLifetime(ServiceLifetime.Scoped);
        });
        services.AddScoped<IRequestHandler<SpyRequest, SpyResponse>>(sp => new SpyRequestHandler(sp.GetRequiredService<List<string>>()));
        services.AddScoped<IPipelineBehavior<SpyRequest, SpyResponse>>(sp => new PassThroughBehavior<SpyRequest, SpyResponse>(sp.GetRequiredService<List<string>>()));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var request = new SpyRequest();

        // Act
        await mediator.Send(request);

        // Assert
        executionLog.Should().Contain(SpyRequestHandler.CalledMarker);
        executionLog.Should().Contain(log => log.StartsWith("PassThroughBehavior"));
    }

    [Fact]
    public async Task Behavior_ShouldThrowException_WhenFailingBehaviorRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddM3diator(cfg =>
             cfg.RegisterServicesFromAssembly(typeof(IntegrationTests).Assembly)
                .AddOpenBehavior(typeof(FailingBehavior<,>))
                .WithLifetime(ServiceLifetime.Scoped)
        );
        services.AddScoped<IRequestHandler<SpyRequest, SpyResponse>, SpyRequestHandler>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var act = async () => await mediator.Send(new SpyRequest());
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage(FailingBehavior<SpyRequest, SpyResponse>.ExceptionMessage);
    }

    [Fact]
    public async Task Behavior_ShouldUseLoggerFromDI_WhenInjected()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingBehavior<SpyRequest, SpyResponse>>>();
        var services = new ServiceCollection();

        services.AddSingleton(mockLogger.Object);
        services.AddLogging();

        services.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<IMediator>();
            cfg.WithLifetime(ServiceLifetime.Scoped);
        });

        services.AddScoped<IRequestHandler<SpyRequest, SpyResponse>, SpyRequestHandler>();

        services.AddScoped<IPipelineBehavior<SpyRequest, SpyResponse>>(sp =>
            new LoggingBehavior<SpyRequest, SpyResponse>(mockLogger.Object, null)
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new SpyRequest());

        // Assert
        mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => CheckLogState(state, "Handling request {RequestType}", "SpyRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        mockLogger.Verify(
             logger => logger.Log(
                 LogLevel.Information,
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((state, type) => CheckLogState(state, "Finished handling {RequestType}, returning {ResponseType}", "SpyResponse")),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);
    }

    private bool CheckLogState(object state, string expectedTemplate, params string[] expectedParamValues)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object>> loggedValues)
        {
            var templatePair = loggedValues.FirstOrDefault(kvp => kvp.Key == "{OriginalFormat}");
            if (templatePair.Value?.ToString() != expectedTemplate) return false;
            foreach (var expectedVal in expectedParamValues)
            {
                if (!loggedValues.Any(kvp => kvp.Key != "{OriginalFormat}" && kvp.Value?.ToString() == expectedVal)) return false;
            }
            return true;
        }
        return false;
    }

    [Fact]
    public async Task Publish_ShouldInvokeAllRegisteredNotificationHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IMediator>()
               .WithLifetime(ServiceLifetime.Scoped)
        );

        services.AddScoped<INotificationHandler<SpyNotification>>(sp =>
            new SpyNotificationHandler(sp.GetRequiredService<List<string>>()));

        services.AddScoped<INotificationHandler<SpyNotification>>(sp =>
             new SpyNotificationHandler2(sp.GetRequiredService<List<string>>()));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var notification = new SpyNotification();

        // Act
        await mediator.Publish(notification);

        // Assert
        executionLog.Should().Contain(SpyNotificationHandler.CalledMarker);
        executionLog.Should().Contain(SpyNotificationHandler2.CalledMarker);
        executionLog.Should().HaveCount(2);
    }

    [Fact]
    public async Task Publish_WithOneHandlerFailing_ShouldInvokeOtherHandlersAndThrowAggregateException()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IMediator>()
               .WithLifetime(ServiceLifetime.Scoped)
        );

        services.AddScoped<INotificationHandler<SpyNotification>>(sp =>
            new SpyNotificationHandler(sp.GetRequiredService<List<string>>()));

        services.AddScoped<INotificationHandler<SpyNotification>>(sp =>
            new FailingSpyNotificationHandler(sp.GetRequiredService<List<string>>()));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var notification = new SpyNotification();

        // Act & Assert
        var act = async () => await mediator.Publish(notification);
        var exceptionAssertion = await act.Should().ThrowAsync<AggregateException>();
        var aggEx = exceptionAssertion.Which;

        executionLog.Should().Contain(SpyNotificationHandler.CalledMarker);
        executionLog.Should().Contain(FailingSpyNotificationHandler.CalledMarker);

        aggEx.InnerExceptions.Should().ContainSingle()
               .Which.Should().BeOfType<InvalidOperationException>()
               .Which.Message.Should().Be(FailingSpyNotificationHandler.FailureMessage);
    }

    [Fact]
    public async Task Send_ObjectRequest_Integration_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IMediator>()
               .WithLifetime(ServiceLifetime.Scoped)
        );

        services.AddScoped<IRequestHandler<SpyRequest, SpyResponse>>(sp =>
            new SpyRequestHandler(sp.GetRequiredService<List<string>>())
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new SpyRequest() as object;

        // Act
        var result = await mediator.Send(request);

        // Assert
        executionLog.Should().Contain(SpyRequestHandler.CalledMarker);
        result.Should().NotBeNull().And.BeOfType<SpyResponse>();
    }

    [Fact]
    public async Task Publish_ObjectNotification_Integration_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IMediator>()
               .WithLifetime(ServiceLifetime.Scoped)
        );

        services.AddScoped<INotificationHandler<SpyNotification>>(sp =>
            new SpyNotificationHandler(sp.GetRequiredService<List<string>>())
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var notification = new SpyNotification() as object;

        // Act
        await mediator.Publish(notification);

        // Assert
        executionLog.Should().Contain(SpyNotificationHandler.CalledMarker);
        executionLog.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateStream_Integration_WithRegisteredHandler_ShouldReturnStream()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();

        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IntegrationTests>()
               .WithLifetime(ServiceLifetime.Transient)
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new StreamSequenceRequest(4, SimulateDelayMs: 1);
        var expectedValues = new[] { 1, 2, 3, 4 };
        var receivedItems = new List<SequenceItem>();

        // Act
        var stream = mediator.CreateStream(request);
        await foreach (var item in stream)
        {
            receivedItems.Add(item);
        }

        // Assert
        receivedItems.Select(i => i.Value).Should().BeEquivalentTo(expectedValues, options => options.WithStrictOrdering());
        executionLog.Should().Contain(StreamSequenceHandler.CalledMarker);
        executionLog.Where(log => log.StartsWith(StreamSequenceHandler.ItemMarker)).Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateStream_Integration_EmptyHandler_ShouldReturnEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();
        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IntegrationTests>()
               .WithLifetime(ServiceLifetime.Transient)
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new EmptyStreamRequest();
        var receivedItems = new List<string>();

        // Act
        var stream = mediator.CreateStream(request);
        await foreach (var item in stream)
        {
            receivedItems.Add(item);
        }

        // Assert
        receivedItems.Should().BeEmpty();
        executionLog.Should().Contain(EmptyStreamHandler.CalledMarker);
    }

    [Fact]
    public async Task CreateStream_Integration_UnregisteredHandler_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<IMediator>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new UnregisteredStreamRequest();

        // Act
        Func<Task> act = async () =>
        {
            var stream = mediator.CreateStream(request);
            await foreach (var _ in stream) { }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage($"*IStreamRequestHandler`2[M3diator.Tests.UnregisteredStreamRequest,System.Int32]*registered*");
    }

    [Fact]
    public async Task CreateStream_Integration_WithCancellation_ShouldCancelStream()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionLog = new List<string>();
        services.AddSingleton(executionLog);
        services.AddLogging();
        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<IntegrationTests>()
               .WithLifetime(ServiceLifetime.Transient)
        );

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = new StreamSequenceRequest(10, SimulateDelayMs: 10);
        using var cts = new CancellationTokenSource();
        var receivedItems = new List<SequenceItem>();
        int itemsToReceive = 2;

        // Act & Assert
        var stream = mediator.CreateStream(request, cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in stream.WithCancellation(cts.Token))
            {
                receivedItems.Add(item);
                if (receivedItems.Count >= itemsToReceive)
                {
                    cts.Cancel();
                    await Task.Delay(50, CancellationToken.None);
                }
            }
        });

        // Assert
        receivedItems.Should().HaveCount(itemsToReceive);
        receivedItems.Select(i => i.Value).Should().Equal(1, 2);
        executionLog.Should().Contain(StreamSequenceHandler.CalledMarker);
        executionLog.Where(log => log.StartsWith(StreamSequenceHandler.ItemMarker)).Should().HaveCount(itemsToReceive);
    }
}