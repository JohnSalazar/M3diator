using FluentAssertions;
using M3diator.Tests.TestAssemblyMarker;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace M3diator.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddM3diator_ShouldRegisterCoreServices_WithDefaultImplementationAndLifetime()
    {
        var services = new ServiceCollection();
        services.AddM3diator(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<MarkerType>();
            cfg.WithLifetime(ServiceLifetime.Scoped);
        });
        var provider = services.BuildServiceProvider();

        var mediatorDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));
        Assert.NotNull(mediatorDescriptor);
        Assert.True(typeof(IMediator).IsAssignableFrom(mediatorDescriptor.ImplementationType));
        Assert.NotEqual(typeof(MyCustomMediatorForTest), mediatorDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, mediatorDescriptor.Lifetime);
        Assert.NotNull(services.FirstOrDefault(d => d.ServiceType == typeof(ISender)));
        Assert.NotNull(services.FirstOrDefault(d => d.ServiceType == typeof(IPublisher)));

        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetService<ISender>();
        var publisher = scope.ServiceProvider.GetService<IPublisher>();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        Assert.NotNull(sender);
        Assert.NotNull(publisher);
        Assert.NotNull(mediator);
        Assert.Same(mediator, sender);
        Assert.Same(mediator, publisher);
    }

    [Fact]
    public void AddM3diator_ShouldRegisterHandlersAndBehaviors_FromSpecifiedAssemblyAndExplicit()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<MarkerType>()
               .AddOpenBehavior(typeof(LoggingBehavior<,>))
               .WithLifetime(ServiceLifetime.Scoped)
        );

        var reqDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestHandler<TestRequest, TestResponse>));
        Assert.NotNull(reqDesc);
        Assert.Equal(typeof(TestRequestHandler), reqDesc.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, reqDesc.Lifetime);

        var notifDesc = services.FirstOrDefault(d => d.ServiceType == typeof(INotificationHandler<TestNotification>));
        Assert.NotNull(notifDesc);
        Assert.Equal(typeof(TestNotificationHandler), notifDesc.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, notifDesc.Lifetime);

        var openBehDesc = services.Where(d => d.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        Assert.Equal(4, openBehDesc.Count);
        Assert.Contains(openBehDesc, d => d.ImplementationType == typeof(TestPipelineBehavior<,>));
        Assert.Contains(openBehDesc, d => d.ImplementationType == typeof(LoggingBehavior<,>));
        Assert.Contains(openBehDesc, d => d.ImplementationType == typeof(PassThroughBehavior<,>));
        Assert.Contains(openBehDesc, d => d.ImplementationType == typeof(FailingBehavior<,>));
        openBehDesc.ForEach(d => Assert.Equal(ServiceLifetime.Scoped, d.Lifetime));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>());
        var resolvedBehaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<TestRequest, TestResponse>>().ToList();

        Assert.Equal(4, resolvedBehaviors.Count);
        Assert.Contains(resolvedBehaviors, b => b.GetType() == typeof(TestPipelineBehavior<TestRequest, TestResponse>));
        Assert.Contains(resolvedBehaviors, b => b.GetType() == typeof(LoggingBehavior<TestRequest, TestResponse>));
        Assert.Contains(resolvedBehaviors, b => b.GetType() == typeof(PassThroughBehavior<TestRequest, TestResponse>));
        Assert.Contains(resolvedBehaviors, b => b.GetType() == typeof(FailingBehavior<TestRequest, TestResponse>));
    }

    [Fact]
    public void AddM3diator_ShouldRegisterPipelineBehaviors_InResolutionOrderOfRegistration()
    {
        var services = new ServiceCollection();
        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<MarkerType>()
               .AddOpenBehavior(typeof(LoggingBehavior<,>))
               .WithLifetime(ServiceLifetime.Scoped)
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<TestRequest, TestResponse>>().ToList();

        // Assert
        behaviors.Should().HaveCount(4);

        behaviors.Should().Contain(b => b.GetType() == typeof(TestPipelineBehavior<TestRequest, TestResponse>));
        behaviors.Should().Contain(b => b.GetType() == typeof(LoggingBehavior<TestRequest, TestResponse>));
        behaviors.Should().Contain(b => b.GetType() == typeof(PassThroughBehavior<TestRequest, TestResponse>));
        behaviors.Should().Contain(b => b.GetType() == typeof(FailingBehavior<TestRequest, TestResponse>));

        var descriptors = services.Where(d => d.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        descriptors.Should().HaveCount(4);
        descriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }

    [Fact]
    public void AddM3diator_ShouldAllowCustomMediatorImplementation_AndRespectLifetime()
    {
        var services = new ServiceCollection();
        services.AddM3diator(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<MarkerType>()
               .UseMediatorImplementation<MyCustomMediatorForTest>()
               .WithLifetime(ServiceLifetime.Singleton)
        );
        var provider = services.BuildServiceProvider();

        var mediatorDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));
        Assert.NotNull(mediatorDescriptor);
        Assert.Equal(typeof(MyCustomMediatorForTest), mediatorDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, mediatorDescriptor.Lifetime);
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<MyCustomMediatorForTest>(mediator);

        var mediator2 = provider.GetService<IMediator>();
        Assert.Same(mediator, mediator2);
    }

    [Fact]
    public void AddM3diator_NoAssembliesRegistered_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<ArgumentException>("configuration", () => services.AddM3diator(cfg => { }));
        Assert.Contains("No assemblies were specified", exception.Message);
    }

    [Fact]
    public void AddOpenBehavior_NullType_ShouldThrowArgumentNullException()
    {
        var config = new M3diatorServiceConfigurationOptions();
        Assert.Throws<ArgumentNullException>("openBehaviorType", () => config.AddOpenBehavior(null!));
    }

    [Fact]
    public void AddOpenBehavior_NonGenericType_ShouldThrowArgumentException()
    {
        var config = new M3diatorServiceConfigurationOptions();
        var ex = Assert.Throws<ArgumentException>("openBehaviorType", () => config.AddOpenBehavior(typeof(string)));
        Assert.Contains("must be an open generic type definition", ex.Message);
    }

    [Fact]
    public void AddOpenBehavior_ClosedGenericType_ShouldThrowArgumentException()
    {
        var config = new M3diatorServiceConfigurationOptions();
        var closedType = typeof(LoggingBehavior<TestRequest, TestResponse>);
        var ex = Assert.Throws<ArgumentException>("openBehaviorType", () => config.AddOpenBehavior(closedType));
        Assert.Contains("must be an open generic type definition", ex.Message);
    }

    [Fact]
    public void AddOpenBehavior_NotImplementingInterface_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new M3diatorServiceConfigurationOptions();
        var invalidBehaviorType = typeof(List<>);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>("openBehaviorType", () => config.AddOpenBehavior(invalidBehaviorType));

        string expectedSubstring = $"must implement '{typeof(IPipelineBehavior<,>).FullName}'";
        Assert.Contains(expectedSubstring, ex.Message);
    }

    [Fact]
    public void UseMediatorImplementation_NonMediatorTypeViaReflection_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            services.AddM3diator(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<TestAssemblyMarker.MarkerType>();

                var method = cfg.GetType().GetMethod("UseMediatorImplementation");
                Assert.NotNull(method);
                try
                {
                    var genericMethod = method.MakeGenericMethod(typeof(string));
                    genericMethod.Invoke(cfg, null);
                }
                catch (TargetInvocationException tie)
                {
                    throw tie.InnerException!;
                }
            });
        });

        // Assert
        Assert.NotNull(ex);
        Assert.Contains("violates the constraint", ex.Message);
        Assert.Contains("'System.String'", ex.Message);
    }

    public record VoidRequest() : IRequest;
    public class VoidRequestHandler : IRequestHandler<VoidRequest>
    {
        public Task Handle(VoidRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        Task<Unit> IRequestHandler<VoidRequest, Unit>.Handle(VoidRequest request, CancellationToken cancellationToken) => Unit.Task;
    }

    [Fact]
    public void AddM3diator_RegistersVoidRequestHandlerCorrectly()
    {
        var services = new ServiceCollection();
        services.AddM3diator(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensionsTests).Assembly));
        var provider = services.BuildServiceProvider();

        var specificHandlerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestHandler<VoidRequest, Unit>));
        Assert.NotNull(specificHandlerDescriptor);
        Assert.Equal(typeof(VoidRequestHandler), specificHandlerDescriptor.ImplementationType);

        using var scope = provider.CreateScope();
        var specificHandler = scope.ServiceProvider.GetService<IRequestHandler<VoidRequest, Unit>>();
        Assert.NotNull(specificHandler);
        Assert.IsType<VoidRequestHandler>(specificHandler);

        var markerHandler = scope.ServiceProvider.GetService<IRequestHandler<VoidRequest>>();
        Assert.Null(markerHandler);
    }
}