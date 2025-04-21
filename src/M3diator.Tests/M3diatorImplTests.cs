using FluentAssertions;
using M3diator.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace M3diator.Tests;

public class M3diatorImplTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly M3diatorImpl _mediator;

    public M3diatorImplTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _mediator = new M3diatorImpl(_serviceProviderMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns((Type serviceType) => null);
    }

    [Fact]
    public async Task Send_Generic_NoBehavior_ShouldResolveHandlerAndReturnResponse()
    {
        var request = new Ping("Msg");
        var expectedResponse = new Pong("Expected");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution<Ping, Pong>([]);
        var response = await _mediator.Send(request);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
        response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Send_Generic_WithOneBehavior_ShouldExecuteBehaviorAndHandler()
    {
        var request = new Ping("Msg");
        var expectedResponse = new Pong("Expected");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        var mockBehavior = new Mock<IPipelineBehavior<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        mockBehavior.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()))
                  .Returns((Ping req, RequestHandlerDelegate<Pong> next, CancellationToken ct) => next());
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution([mockBehavior.Object]);
        var response = await _mediator.Send(request);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
        mockBehavior.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()), Times.Once);
        response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Send_Generic_WithMultipleBehaviors_ShouldExecuteInReverseRegistrationOrder()
    {
        // Arrange
        var request = new Ping("Msg");
        var expectedResponse = new Pong("Expected");
        var executionOrder = new List<string>();
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        var mockBehavior1 = new Mock<IPipelineBehavior<Ping, Pong>>();
        var mockBehavior2 = new Mock<IPipelineBehavior<Ping, Pong>>();

        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResponse)
                   .Callback(() => executionOrder.Add("Handler"));

        mockBehavior1.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()))
                     .Callback(() => executionOrder.Add("Behavior1"))
                     .Returns((Ping req, RequestHandlerDelegate<Pong> next, CancellationToken ct) => next());

        mockBehavior2.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()))
                     .Callback(() => executionOrder.Add("Behavior2"))
                     .Returns((Ping req, RequestHandlerDelegate<Pong> next, CancellationToken ct) => next());

        SetupHandlerResolution(mockHandler);

        SetupBehaviorResolution([mockBehavior1.Object, mockBehavior2.Object]);

        // Act
        var response = await _mediator.Send(request);

        // Assert
        executionOrder.Should().Equal("Behavior1", "Behavior2", "Handler");
        response.Should().BeEquivalentTo(expectedResponse);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
        mockBehavior1.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockBehavior2.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_HandlerThrowsException_ShouldPropagateException()
    {
        var request = new Ping("Fail");
        var exception = new InvalidOperationException("Handler Failed");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution<Ping, Pong>([]);
        await _mediator.Invoking(m => m.Send(request)).Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Handler Failed");
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_BehaviorThrowsException_ShouldPropagateAndNotCallNextOrHandler()
    {
        // Arrange
        var request = new Ping("Fail");
        var exception = new InvalidOperationException("Behavior Failed");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        var mockBehavior1 = new Mock<IPipelineBehavior<Ping, Pong>>();
        var mockBehavior2 = new Mock<IPipelineBehavior<Ping, Pong>>();

        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>())).ReturnsAsync(new Pong("Not Called"));

        mockBehavior1.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()))
                     .Returns((Ping req, RequestHandlerDelegate<Pong> next, CancellationToken ct) => next());

        mockBehavior2.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(exception);

        SetupHandlerResolution(mockHandler);

        SetupBehaviorResolution([mockBehavior1.Object, mockBehavior2.Object]);

        // Act & Assert
        await _mediator.Invoking(m => m.Send(request))
                       .Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Behavior Failed");

        mockBehavior1.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockBehavior2.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Send_VoidRequest_ShouldResolveHandlerAndComplete()
    {
        var request = new VoidRequest();
        var mockHandler = new Mock<IRequestHandler<VoidRequest, Unit>>();
        mockHandler.Setup(h => h.Handle(request, It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);
        SetupHandlerResolution<VoidRequest, Unit>(mockHandler);
        SetupBehaviorResolution<VoidRequest, Unit>([]);
        var act = async () => await _mediator.Send(request);
        await act.Should().NotThrowAsync();
        mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Send_ObjectRequest_ShouldUseReflectionAndExecuteHandler()
    {
        var request = new Ping("Obj") as object;
        var expectedResponse = new Pong("Expected");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(It.Is<Ping>(r => r.Message == "Obj"), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution<Ping, Pong>([]);
        var response = await _mediator.Send(request);
        mockHandler.Verify(h => h.Handle(It.Is<Ping>(r => r.Message == "Obj"), It.IsAny<CancellationToken>()), Times.Once);
        response.Should().BeEquivalentTo(expectedResponse);
    }
    [Fact]
    public async Task Send_ObjectRequest_Void_ShouldWorkAndReturnUnit()
    {
        var request = new VoidRequest() as object;
        var mockHandler = new Mock<IRequestHandler<VoidRequest, Unit>>();
        mockHandler.Setup(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);
        SetupHandlerResolution<VoidRequest, Unit>(mockHandler);
        SetupBehaviorResolution<VoidRequest, Unit>([]);
        var result = await _mediator.Send(request);
        mockHandler.Verify(h => h.Handle(It.IsAny<VoidRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<Unit>().And.Be(Unit.Value);
    }

    [Fact]
    public async Task Send_ObjectRequest_WhenNotIRequest_ShouldThrowArgumentException()
    {
        var notARequest = new { Message = "Not a request" };
        await _mediator.Invoking(m => m.Send((object)notARequest)).Should().ThrowAsync<ArgumentException>().WithMessage($"Object '{notARequest.GetType().FullName}' does not implement IRequest or IRequest<TResponse>.*");
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
    public async Task Send_WithCancellation_ShouldPassTokenToHandlerAndBehaviors()
    {
        var request = new Ping("Cancel");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        var mockBehavior = new Mock<IPipelineBehavior<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(request, token)).ReturnsAsync(new Pong("OK"));
        mockBehavior.Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), token)).Returns((Ping req, RequestHandlerDelegate<Pong> next, CancellationToken ct) => next());
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution([mockBehavior.Object]);
        await _mediator.Send(request, token);
        mockHandler.Verify(h => h.Handle(request, token), Times.Once);
        mockBehavior.Verify(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<Pong>>(), token), Times.Once);
    }

    [Fact]
    public async Task Publish_NoHandlersRegistered_ShouldCompleteSilently()
    {
        var notification = new SpyNotification();
        SetupNotificationHandlerResolution<SpyNotification>([]);

        await _mediator.Invoking(m => m.Publish(notification)).Should().NotThrowAsync();

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Publish_OneHandler_ShouldExecuteHandler()
    {
        var notification = new SpyNotification();
        var mockHandler = new Mock<INotificationHandler<SpyNotification>>();
        mockHandler.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        SetupNotificationHandlerResolution([mockHandler.Object]);

        await _mediator.Publish(notification);

        mockHandler.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Publish_MultipleHandlers_ShouldExecuteAllHandlers()
    {
        var notification = new SpyNotification();
        var mockHandler1 = new Mock<INotificationHandler<SpyNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<SpyNotification>>();
        mockHandler1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockHandler2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        SetupNotificationHandlerResolution([mockHandler1.Object, mockHandler2.Object]);

        await _mediator.Publish(notification);

        mockHandler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Publish_OneHandlerThrows_ShouldExecuteOthersAndThrowAggregateException()
    {
        var notification = new SpyNotification();
        var exception = new InvalidOperationException("Handler Failed");
        var mockHandlerOK = new Mock<INotificationHandler<SpyNotification>>();
        var mockHandlerFail = new Mock<INotificationHandler<SpyNotification>>();
        mockHandlerOK.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockHandlerFail.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        SetupNotificationHandlerResolution([mockHandlerOK.Object, mockHandlerFail.Object]);

        var act = async () => await _mediator.Publish(notification);

        var exceptionAssertion = await act.Should().ThrowAsync<AggregateException>();
        var aggregateException = exceptionAssertion.Which;
        mockHandlerOK.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        mockHandlerFail.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        aggregateException.InnerExceptions.Should().ContainSingle().Which.Should().Be(exception);

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Publish_ObjectNotification_ShouldWork()
    {
        var notification = new SpyNotification() as object;
        var mockHandler = new Mock<INotificationHandler<SpyNotification>>();
        mockHandler.Setup(h => h.Handle(It.IsAny<SpyNotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        SetupNotificationHandlerResolution<SpyNotification>([mockHandler.Object]);

        await _mediator.Publish(notification);

        mockHandler.Verify(h => h.Handle(It.IsAny<SpyNotification>(), It.IsAny<CancellationToken>()), Times.Once);

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Publish_ObjectNotification_WhenNotINotification_ShouldThrowArgumentException()
    {
        var notANotification = new { Data = "Some data" };
        await _mediator.Invoking(m => m.Publish((object)notANotification))
                      .Should().ThrowAsync<ArgumentException>().WithMessage($"Object '{notANotification.GetType().FullName}' does not implement INotification.*");
    }

    [Fact]
    public async Task Publish_WithCancellation_ShouldPassTokenToHandlers()
    {
        var notification = new SpyNotification();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var mockHandler1 = new Mock<INotificationHandler<SpyNotification>>();
        var mockHandler2 = new Mock<INotificationHandler<SpyNotification>>();
        mockHandler1.Setup(h => h.Handle(notification, token)).Returns(Task.CompletedTask);
        mockHandler2.Setup(h => h.Handle(notification, token)).Returns(Task.CompletedTask);
        SetupNotificationHandlerResolution([mockHandler1.Object, mockHandler2.Object]);

        await _mediator.Publish(notification, token);

        mockHandler1.Verify(h => h.Handle(notification, token), Times.Once);
        mockHandler2.Verify(h => h.Handle(notification, token), Times.Once);

        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once);
    }

    [Fact]
    public async Task Send_CalledTwice_ShouldHitCacheAndCallGetRequiredServiceOnce()
    {
        // Arrange
        var request1 = new Ping("First Call");
        var request2 = new Ping("Second Call");
        var mockHandler = new Mock<IRequestHandler<Ping, Pong>>();
        mockHandler.Setup(h => h.Handle(It.IsAny<Ping>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Pong("Cached?"));
        SetupHandlerResolution(mockHandler);
        SetupBehaviorResolution<Ping, Pong>([]);

        // Act
        await _mediator.Send(request1);
        await _mediator.Send(request2);

        // Assert
        _serviceProviderMock.Verify(sp => sp.GetService(typeof(IRequestHandler<Ping, Pong>)), Times.Once());
        mockHandler.Verify(h => h.Handle(request1, It.IsAny<CancellationToken>()), Times.Once);
        mockHandler.Verify(h => h.Handle(request2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_CalledTwice_ShouldHitCacheAndCallGetServicesOnce()
    {
        // Arrange
        var notification1 = new SpyNotification();
        var notification2 = new SpyNotification();
        var mockHandler = new Mock<INotificationHandler<SpyNotification>>();
        mockHandler.Setup(h => h.Handle(It.IsAny<SpyNotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        SetupNotificationHandlerResolution<SpyNotification>([mockHandler.Object]);

        // Act
        await _mediator.Publish(notification1);
        await _mediator.Publish(notification2);

        // Assert
        var expectedEnumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(INotificationHandler<SpyNotification>));
        _serviceProviderMock.Verify(sp => sp.GetService(expectedEnumerableType), Times.Once());

        mockHandler.Verify(h => h.Handle(It.IsAny<SpyNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void SetupHandlerResolution<TRequest, TResponse>(Mock<IRequestHandler<TRequest, TResponse>> mockHandler)
        where TRequest : IRequest<TResponse> => SetupHandlerResolution(mockHandler.Object);

    private void SetupHandlerResolution<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handlerInstance)
        where TRequest : IRequest<TResponse>
    {
        var handlerServiceType = typeof(IRequestHandler<TRequest, TResponse>);

        _serviceProviderMock.Setup(sp => sp.GetService(handlerServiceType))
                            .Returns(handlerInstance);
    }

    private void SetupBehaviorResolution<TRequest, TResponse>(IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
         where TRequest : IRequest<TResponse>
    {
        var behaviorServiceType = typeof(IPipelineBehavior<,>).MakeGenericType(typeof(TRequest), typeof(TResponse));
        var behaviorEnumerableType = typeof(IEnumerable<>).MakeGenericType(behaviorServiceType);

        _serviceProviderMock.Setup(sp => sp.GetService(behaviorEnumerableType))
                            .Returns(behaviors);
    }

    private void SetupNotificationHandlerResolution<TNotification>(IEnumerable<INotificationHandler<TNotification>> handlers)
       where TNotification : INotification
    {
        var handlerServiceType = typeof(INotificationHandler<TNotification>);
        var handlerEnumerableType = typeof(IEnumerable<>).MakeGenericType(handlerServiceType);

        _serviceProviderMock.Setup(sp => sp.GetService(handlerEnumerableType))
                           .Returns(handlers);
    }
}