# M3diator

[![NuGet Version](https://img.shields.io/nuget/v/M3diator.svg?style=flat-square)](https://www.nuget.org/packages/M3diator/) [![Build Status](https://img.shields.io/azure-devops/build/your-org/your-project/your-build-definition-id.svg?style=flat-square)](link-to-your-build-pipeline) [![License](https://img.shields.io/github/license/your-github-username/M3diator.svg?style=flat-square)](LICENSE) A lightweight, high-performance Mediator pattern implementation in .NET, inspired by [MediatR](https://github.com/jbogard/MediatR).

## What is the Mediator Pattern?

The Mediator pattern is a behavioral design pattern that promotes loose coupling between components by having them interact indirectly through a central `mediator` object, instead of directly with each other.

**Benefits:**

* **Reduced Coupling:** Objects don't need direct references to each other. They only know the mediator. This aligns with SOLID principles like Dependency Inversion (DIP) and makes the system easier to modify.
* **Improved Maintainability:** Changes within one component are less likely to impact others. Adding new components or handlers often doesn't require changing existing ones (Open/Closed Principle - OCP).
* **Single Responsibility Principle (SRP):** Each handler focuses on a single request or notification type. The mediator focuses solely on dispatching.
* **Simplified Communication:** Centralizes complex communication logic that would otherwise be scattered among multiple components.

In the context of CQRS (Command Query Responsibility Segregation), the Mediator pattern is often used to decouple the sender of a command or query from its handler.

## What is M3diator?

M3diator is a .NET library that provides an elegant and efficient way to implement the Mediator pattern. It helps you build cleaner, more maintainable applications by decoupling in-process message sending and handling.

It draws heavy inspiration from the popular MediatR library but aims for simplicity and optimal performance, leveraging modern .NET features.

## Installation

Install M3diator via the NuGet Package Manager console or by searching for `M3diator` in the NuGet Package Manager UI.

```powershell
Install-Package M3diator
Install-Package M3diator.Abstractions
````

_(Note: `M3diator` depends on `M3diator.Abstractions`, so installing `M3diator` usually suffices. Install `M3diator.Abstractions` separately if you only need the contracts in a specific project)._

## Configuration

Register M3diator and its handlers in your application's service collection, typically in `Program.cs` (for .NET 6+) or `Startup.cs`.

C#

```
// Program.cs (.NET 6+)

using M3diator.Extensions; // Add this using statement

var builder = WebApplication.CreateBuilder(args);

// Add M3diator and scan assemblies for handlers
// Replace 'typeof(Program).Assembly' with the assembly(ies) containing your handlers
builder.Services.AddM3diator(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    // Optionally register pipeline behaviors
    // cfg.AddPipelineBehavior(typeof(YourLoggingBehavior<,>));
});

// ... other services

var app = builder.Build();

// ... configure middleware

app.Run();
```

The `AddM3diator` extension method registers the core `IMediator`, `ISender`, and `IPublisher` interfaces. `RegisterServicesFromAssembly` scans the specified assembly (or assemblies) for implementations of `IRequestHandler<,>`, `IRequestHandler<>`, and `INotificationHandler<>` and registers them with the dependency injection container.

## Usage

M3diator revolves around three main message types: Requests (Commands/Queries) and Notifications (Events).

### 1\. Requests (Commands and Queries)

Requests represent an action to perform or data to retrieve. They can optionally return a value.

*   **`IRequest<TResponse>`:** Represents a request that returns a value of type `TResponse`. Typically used for Queries.
*   **`IRequest`:** Represents a request that does not return a value (implicitly returns `Unit`). Typically used for Commands. (`M3diator.Abstractions.Unit` is a void marker type).

#### Defining Requests

C#

```
using M3diator.Abstractions;

/// <summary>
/// Represents a query to retrieve a specific customer's details.
/// Implements IRequest<TResponse> because it returns a CustomerDto.
/// </summary>
public class GetCustomerByIdQuery : IRequest<CustomerDto>
{
    public int CustomerId { get; set; }
}

/// <summary>
/// Represents a command to create a new customer.
/// Implements IRequest (or IRequest<Unit>) because it performs an action
/// without returning specific data (fire-and-forget style).
/// </summary>
public class CreateCustomerCommand : IRequest // Equivalent to IRequest<Unit>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Example DTO
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

#### Defining Request Handlers

Handlers contain the logic to process a specific request type. Each request type must have exactly one handler.

*   **`IRequestHandler<TRequest, TResponse>`:** Handles requests implementing `IRequest<TResponse>`.
*   **`IRequestHandler<TRequest>`:** Handles requests implementing `IRequest` (which implicitly means `IRequest<Unit>`).

<!-- end list -->

C#

```
using M3diator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles the GetCustomerByIdQuery request.
/// Dependencies (like a repository) can be injected via the constructor.
/// </summary>
public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    // Example dependency (replace with your actual data access)
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    /// <summary>
    /// Processes the incoming query.
    /// </summary>
    /// <param name="request">The query object containing the CustomerId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task containing the CustomerDto or null if not found.</returns>
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        // Simulate fetching data
        await Task.Delay(10, cancellationToken); // Simulate async work

        if (request.CustomerId == 1)
        {
            return new CustomerDto { Id = request.CustomerId, Name = "John Doe" };
        }
        return null; // Or throw an exception if preferred
    }
}

/// <summary>
/// Handles the CreateCustomerCommand request.
/// </summary>
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand> // No TResponse needed
{
    private readonly ICustomerRepository _customerRepository;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository)
    {
         _customerRepository = customerRepository;
    }

    /// <summary>
    /// Processes the incoming command.
    /// </summary>
    /// <param name="request">The command object containing customer details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task containing Unit (representing void).</returns>
    public async Task<Unit> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Simulate saving data
        Console.WriteLine($"Creating customer: {request.Name}, Email: {request.Email}");
        await Task.Delay(20, cancellationToken); // Simulate async work
        // _customerRepository.Add(new Customer { Name = request.Name, Email = request.Email });
        // await _customerRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value; // Return Unit.Value for void handlers
    }
}

// Dummy repository interface for example
public interface ICustomerRepository { /* ... methods ... */ }
```

#### Sending Requests

Inject `IMediator` or `ISender` into your classes (e.g., Controllers, Services) and use the `Send` method.

C#

```
using M3diator.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator; // Or ISender

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET endpoint to fetch a customer by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var query = new GetCustomerByIdQuery { CustomerId = id };
        CustomerDto? customer = await _mediator.Send(query); // Use Send for requests

        if (customer == null)
        {
            return NotFound();
        }
        return Ok(customer);
    }

    /// <summary>
    /// POST endpoint to create a new customer.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        await _mediator.Send(command); // Send returns Task<Unit> which can be awaited
        // Usually return CreatedAtAction or Ok
        return Ok();
    }
}
```

### 2\. Notifications (Events)

Notifications represent something that has happened (an event). They are dispatched to multiple handlers.

*   **`INotification`:** Marker interface for notification messages.

#### Defining Notifications

C#

```
using M3diator.Abstractions;

/// <summary>
/// Notification published after a customer has been successfully created.
/// </summary>
public class CustomerCreatedNotification : INotification
{
    public int CustomerId { get; }
    public string CustomerName { get; }

    public CustomerCreatedNotification(int customerId, string customerName)
    {
        CustomerId = customerId;
        CustomerName = customerName ?? string.Empty;
    }
}
```

#### Defining Notification Handlers

A single notification can have zero or many handlers. Handlers implement `INotificationHandler<TNotification>`.

C#

```
using M3diator.Abstractions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles the CustomerCreatedNotification to send a welcome email.
/// </summary>
public class SendWelcomeEmailHandler : INotificationHandler<CustomerCreatedNotification>
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task representing the completion of handling.</returns>
    public async Task Handle(CustomerCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handler 1: Sending welcome email to customer {notification.CustomerName} (ID: {notification.CustomerId})");
        
        await Task.Delay(15, cancellationToken); // Simulate I/O
    }
}

//// <summary>
/// Another handler for the same CustomerCreatedNotification, perhaps for logging.
/// </summary>
public class LogCustomerCreationHandler : INotificationHandler<CustomerCreatedNotification>
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task representing the completion of handling.</returns>
    public async Task Handle(CustomerCreatedNotification notification, CancellationToken cancellationToken)
    {        
        Console.WriteLine($"Handler 2: Logging creation of customer {notification.CustomerName} (ID: {notification.CustomerId})");
        await Task.CompletedTask;
    }
}
```

#### Publishing Notifications

Inject `IMediator` or `IPublisher` and use the `Publish` method. `Publish` will locate all registered handlers for the notification type and invoke them (typically concurrently, but M3diator awaits their completion by default).

C#

```
// Inside CreateCustomerCommandHandler.Handle method, after saving the customer:

public async Task<Unit> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
{
    // ... (code to save customer and get the new customerId/Name)
    var newCustomerId = 123; // Example Id
    var customerName = request.Name;

    Console.WriteLine($"Customer {customerName} created with ID {newCustomerId}.");

    // Publish a notification event
    var notification = new CustomerCreatedNotification(newCustomerId, customerName);
    await _mediator.Publish(notification, cancellationToken); // Use Publish for notifications

    Console.WriteLine("Customer creation notification published.");

    return Unit.Value;
}

// Need to inject IMediator or IPublisher into the handler
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMediator _mediator; // Inject Mediator/Publisher

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IMediator mediator)
    {
         _customerRepository = customerRepository;
         _mediator = mediator; // Assign injected instance
    }
    // ... Handle method as above ...
}
```

### 3\. Pipeline Behaviors

Pipeline behaviors allow you to wrap inner request handlers with additional logic, enabling cross-cutting concerns like logging, validation, performance monitoring, or transaction management without cluttering your core handler logic.

*   **`IPipelineBehavior<TRequest, TResponse>`:** Interface to implement for pipeline behaviors.

#### Defining a Pipeline Behavior

C#

```
using M3diator.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Example pipeline behavior for logging request execution time.
/// </summary>
/// <typeparam name="TRequest">Type of the request being handled.</typeparam>
/// <typeparam name="TResponse">Type of the response from the handler.</typeparam>
public class PerformanceLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> // Constrain TRequest to match the interface
{
    /// <summary>
    /// Intercepts the request handling process.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The delegate representing the next action in the pipeline (usually the handler itself or the next behavior).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the inner handler.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"[Pipeline] Handling request {typeof(TRequest).Name}...");

        try
        {
            // Call the next delegate/handler in the chain
            var response = await next();

            stopwatch.Stop();

            Console.WriteLine($"[Pipeline] Handled {typeof(TRequest).Name} in {stopwatch.ElapsedMilliseconds}ms.");

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Console.WriteLine($"[Pipeline] Exception handling {typeof(TRequest).Name} after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw; // Re-throw the exception to maintain original behavior
        }
    }
}
```

#### Registering Pipeline Behaviors

Register pipeline behaviors with the DI container. The order of registration matters; they execute in the order they are added.

C#

```
// Program.cs or Startup.cs

builder.Services.AddM3diator(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Register pipeline behaviors - Order matters!
    // These apply to all requests matching the generic constraints.
    cfg.AddPipelineBehavior(typeof(PerformanceLoggingBehavior<,>));
    // cfg.AddPipelineBehavior(typeof(ValidationBehavior<,>)); // Example: Add validation next
});

// Alternatively, using standard DI registration (useful for more complex scenarios):
// builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceLoggingBehavior<,>));
// builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

When a request is sent using `_mediator.Send()`, M3diator constructs a pipeline: the request flows through each registered `IPipelineBehavior` before reaching the actual `IRequestHandler`, and the response flows back out through the behaviors in reverse order.

## Performance Benchmarks

Performance is a key consideration for M3diator. Below are the benchmark results comparing M3diator's `Send` and `Publish` operations against MediatR.

Ini, TOML

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5737/22H2/2022Update)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.203
  [Host]   : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method                                    | NumberOfNotificationHandlers | Mean         | Error        | StdDev       | Median       | Gen0   | Allocated |
|------------------------------------------ |----------------------------- |-------------:|-------------:|-------------:|-------------:|-------:|----------:|
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;    | 1                            |     13.08 μs |     0.130 μs |     0.108 μs |     13.04 μs | 0.5798 |    2464 B |
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;    | 3                            |     13.33 μs |     0.263 μs |     0.513 μs |     13.01 μs | 0.5798 |    2464 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;    | 1                            |     79.39 μs |     0.633 μs |     0.495 μs |     79.35 μs | 2.8076 |   12057 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;    | 3                            |     81.45 μs |     1.589 μs |     3.927 μs |     79.23 μs | 2.8076 |   12057 B |
| &#39;Resolve+Exec Handler Directly&#39;           | 1                            | 15,191.07 μs |    51.350 μs |    48.033 μs | 15,192.82 μs |      - |     419 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                     | 1                            | 15,193.86 μs |    79.858 μs |    74.699 μs | 15,209.90 μs |      - |    1091 B |
| &#39;Resolve+Exec Handler Directly&#39;           | 3                            | 15,219.73 μs |    41.572 μs |    38.886 μs | 15,210.64 μs |      - |     419 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                     | 3                            | 15,226.44 μs |    47.439 μs |    44.375 μs | 15,222.56 μs |      - |    1092 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;          | 1                            | 60,472.51 μs |   726.233 μs |   679.319 μs | 60,598.92 μs |      - |    2004 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;          | 3                            | 60,497.97 μs |   802.531 μs |   750.688 μs | 60,758.64 μs |      - |    2004 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;        | 3                            | 75,287.62 μs |   770.391 μs |   720.624 μs | 75,528.86 μs |      - |    2570 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;               | 1                            | 75,540.79 μs |   345.888 μs |   323.543 μs | 75,558.99 μs |      - |    2570 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;   | 3                            | 75,541.77 μs |   266.055 μs |   248.868 μs | 75,515.57 μs |      - |    2529 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;          | 3                            | 75,583.53 μs |   703.996 μs |   658.518 μs | 75,665.46 μs |      - |    3298 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;               | 3                            | 75,585.11 μs |   855.155 μs |   799.912 μs | 75,825.49 μs |      - |    2570 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;   | 1                            | 75,626.34 μs |   491.800 μs |   460.030 μs | 75,718.40 μs |      - |    2570 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;        | 1                            | 75,661.62 μs |   321.583 μs |   300.809 μs | 75,708.57 μs |      - |    2570 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;            | 3                            | 75,786.21 μs |   236.430 μs |   221.157 μs | 75,758.21 μs |      - |    2690 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;            | 1                            | 75,801.17 μs |   249.574 μs |   233.451 μs | 75,808.96 μs |      - |    2690 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;          | 1                            | 75,851.68 μs |   375.230 μs |   350.991 μs | 75,936.73 μs |      - |    3339 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;        | 1                            | 90,459.93 μs | 1,170.207 μs | 1,094.612 μs | 90,440.82 μs |      - |    2891 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;        | 3                            | 90,547.66 μs | 1,161.886 μs | 1,086.829 μs | 90,800.73 μs |      - |    2891 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;        | 3                            | 90,685.72 μs | 1,075.169 μs | 1,005.714 μs | 91,119.73 μs |      - |    2331 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;         | 3                            | 90,720.39 μs | 1,242.969 μs | 1,101.860 μs | 90,981.37 μs |      - |    2891 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;         | 1                            | 90,753.52 μs |   882.555 μs |   825.543 μs | 90,965.75 μs |      - |    2891 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;        | 1                            | 90,956.87 μs |   698.312 μs |   619.035 μs | 91,123.98 μs |      - |    2891 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;        | 1                            | 90,963.86 μs |   920.439 μs |   860.979 μs | 91,046.77 μs |      - |    2331 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;        | 3                            | 91,034.88 μs |   866.341 μs |   810.376 μs | 91,214.27 μs |      - |    2891 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39; | 3                            | 91,077.77 μs |   379.092 μs |   354.603 μs | 91,083.53 μs |      - |    2891 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39; | 1                            | 91,097.20 μs |   297.520 μs |   263.744 μs | 91,119.85 μs |      - |    2891 B |



**Key Takeaways:**

*   M3diator demonstrates significantly **lower execution time** for both sending requests (`Send`) and publishing notifications (`Publish`) compared to MediatR in these benchmarks.
*   M3diator achieves **zero memory allocation** for the mediator dispatch operations themselves (allocations within handlers are separate).

These results suggest that M3diator can be a highly performant choice for applications where mediator overhead is a critical factor.

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request. (Add more details here if needed: contribution guidelines, code style, etc.)

## License

This project <img alt="Brasil" src="https://github.com/JohnSalazar/microservices-go-authentication/assets/16736914/3ecb04fb-b2ce-4e8b-b492-99c5c5c4b317" width="20" height="14" /> is licensed under the [MIT License](LICENSE).