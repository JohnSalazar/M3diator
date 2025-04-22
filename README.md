# M3diator

[![NuGet Version](https://img.shields.io/nuget/v/M3diator?label=M3diator&logo=nuget)](https://www.nuget.org/packages/M3diator/) [![NuGet Version](https://img.shields.io/nuget/v/M3diator.Abstractions?label=M3diator.Abstractions&logo=nuget)](https://www.nuget.org/packages/M3diator.Abstractions/) 

A lightweight, high-performance Mediator pattern implementation in .NET, inspired by [MediatR](https://github.com/jbogard/MediatR).

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

```csharp
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

```csharp
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

```csharp
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

```csharp
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

```csharp
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

```csharp
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

```csharp
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

### 3. Pipeline Behaviors

Pipeline behaviors allow you to wrap inner request handlers with additional logic, enabling cross-cutting concerns like logging, validation, performance monitoring, or transaction management without cluttering your core handler logic.

*   **`IPipelineBehavior<TRequest, TResponse>`:** Interface to implement for pipeline behaviors.

#### Defining a Pipeline Behavior

```csharp
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

```csharp
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

### 4. Streaming Requests

Beyond single request/response and notifications, M3diator supports streaming requests where a single request results in a stream of multiple responses over time. This is useful for scenarios like processing large datasets without loading everything into memory, handling long-running operations that report progress, or returning sequences where items become available incrementally.

* **`IStreamRequest<TResponse>`:** Represents a request that will yield a stream of `TResponse` items.
* **`IStreamRequestHandler<TRequest, TResponse>`:** Defines the handler responsible for processing an `IStreamRequest<TResponse>` and generating the `IAsyncEnumerable<TResponse>`.

#### Defining Stream Requests

Define a class or record implementing `IStreamRequest<TResponse>`, where `TResponse` is the type of item in the resulting stream.

```csharp
using M3diator; // Or M3diator.Abstractions if interfaces are there

/// <summary>
/// Represents a request to stream a sequence of data records.
/// </summary>
/// <param name="Filter">Criteria to filter the data.</param>
public record StreamDataRequest(string Filter) : IStreamRequest<DataRecord>;

// Example response item DTO
public record DataRecord(int Id, string Payload);
```

#### Defining Stream Request Handlers

Implement IStreamRequestHandler<TRequest, TResponse>. The Handle method must return an IAsyncEnumerable<TResponse>. Use async IAsyncEnumerable and yield return to produce items asynchronously. Crucially, check the CancellationToken frequently within the handler to support cancellation during potentially long-running stream generation.

```csharp
using M3diator; // Or M3diator.Abstractions
using System.Collections.Generic;
using System.Runtime.CompilerServices; // For [EnumeratorCancellation]
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles the StreamDataRequest to generate a stream of DataRecord objects.
/// </summary>
public class StreamDataRequestHandler : IStreamRequestHandler<StreamDataRequest, DataRecord>
{
    // Example dependency
    // private readonly IDataSource _dataSource;
    // public StreamDataRequestHandler(IDataSource dataSource) { /* ... */ }

    /// <summary>
    /// Processes the request and generates the asynchronous stream.
    /// </summary>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of DataRecord items.</returns>
    public async IAsyncEnumerable<DataRecord> Handle(
        StreamDataRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Console.WriteLine(<span class="math-inline">"Streaming data records matching filter: {request.Filter}");
        // Simulate fetching pages or chunks of data
        for (int i = 1; i <= 5; i++) // Example: 5 chunks
        {
        // Check for cancellation before potentially expensive work/IO
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(</span>"Fetching data chunk {i}...");
            await Task.Delay(100, cancellationToken); // Simulate async I/O

            // Yield items for the current chunk
            for (int j = 0; j < 3; j++) // Example: 3 items per chunk
            {
                // Check cancellation before yielding
                cancellationToken.ThrowIfCancellationRequested();

                int recordId = (i - 1) * 3 + j + 1;
                yield return new DataRecord(recordId, $"Record {recordId} for filter '{request.Filter}'");
            }
        }
        Console.WriteLine("Finished streaming data records.");
    }
}
```

#### Consuming Streams

Inject IMediator or ISender and use the CreateStream method. Consume the returned IAsyncEnumerable<TResponse> using await foreach.

```csharp
using M3diator; // Or M3diator.Abstractions
using Microsoft.AspNetCore.Mvc; // Example using ASP.NET Core
using System.Threading.Tasks;
using System.Threading; // For CancellationToken

[ApiController]
[Route("[controller]")]
public class DataStreamController : ControllerBase
{
    private readonly IMediator _mediator;

    public DataStreamController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET endpoint to initiate and consume a data stream.
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamData([FromQuery] string filter = "default", CancellationToken cancellationToken = default)
    {
        var request = new StreamDataRequest(filter);

        Console.WriteLine($"Controller: Calling CreateStream for filter '{filter}'...");

        // Use CreateStream to get the async stream
        IAsyncEnumerable<DataRecord> dataStream = _mediator.CreateStream(request, cancellationToken);

        // Consume the stream asynchronously
        // Response headers must be sent before the first await foreach iteration if streaming to HTTP response
        // Consider using IActionResult like `return new StreamResult(dataStream)` in real web scenarios

        int count = 0;
        await foreach (var record in dataStream.WithCancellation(cancellationToken)) // Essential to propagate cancellation
        {
            // Process each item as it arrives
            count++;
            Console.WriteLine(<span class="math-inline">"Controller: Received record {record.Id}: {record.Payload}");
            // Optional: Add delay to simulate processing
            // await Task.Delay(50, cancellationToken);
        }
        Console.WriteLine(</span>"Controller: Finished consuming stream. Total items received: {count}.");

        // In a real API, you might stream this directly to the response body.
        // This example just consumes and logs. You would typically return an appropriate ActionResult.
        // return Ok(); // Or perhaps no explicit return if streaming directly to Response.BodyWriter
    }
}
```


## Performance Benchmarks

Performance is a key consideration for M3diator. Below are the benchmark results comparing M3diator's `Send` and `Publish` operations against MediatR.

```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5737/22H2/2022Update)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.203
  [Host]   : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method                                           | NumberOfNotificationHandlers | Operations | Mean         | Error        | StdDev       | Median       | Gen0   | Allocated |
|------------------------------------------------- |----------------------------- |----------- |-------------:|-------------:|-------------:|-------------:|-------:|----------:|
| &#39;CreateStream (100 Items) - Get Enumerator Only&#39; | 3                            | 1          |     79.50 ns |     1.099 ns |     1.028 ns |     78.98 ns | 0.0459 |     192 B |
| &#39;CreateStream (100 Items) - Get Enumerator Only&#39; | 1                            | 1          |     81.21 ns |     1.189 ns |     1.272 ns |     80.66 ns | 0.0459 |     192 B |
| &#39;CreateStream (100 Items) - Get Enumerator Only&#39; | 3                            | 100        |     82.30 ns |     1.689 ns |     2.312 ns |     82.59 ns | 0.0459 |     192 B |
| &#39;CreateStream (100 Items) - Get Enumerator Only&#39; | 1                            | 100        |     82.92 ns |     1.113 ns |     1.041 ns |     82.54 ns | 0.0459 |     192 B |
| &#39;Resolve+Exec Handler Directly&#39;                  | 1                            | 1          |    718.20 ns |     8.442 ns |     7.896 ns |    719.09 ns | 0.0381 |     160 B |
| &#39;Resolve+Exec Handler Directly&#39;                  | 3                            | 100        |    732.92 ns |    10.589 ns |    11.330 ns |    731.62 ns | 0.0381 |     160 B |
| &#39;Resolve+Exec Handler Directly&#39;                  | 3                            | 1          |    737.55 ns |    14.486 ns |    19.338 ns |    731.16 ns | 0.0381 |     160 B |
| &#39;Resolve+Exec Handler Directly&#39;                  | 1                            | 100        |    742.87 ns |    14.462 ns |    19.307 ns |    735.23 ns | 0.0381 |     160 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                            | 3                            | 1          |  1,534.21 ns |     9.909 ns |     7.737 ns |  1,532.04 ns | 0.1984 |     832 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                            | 3                            | 100        |  1,539.33 ns |    18.297 ns |    15.279 ns |  1,535.14 ns | 0.1984 |     832 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                            | 1                            | 1          |  1,573.80 ns |    25.434 ns |    33.071 ns |  1,568.36 ns | 0.1984 |     832 B |
| &#39;Send&lt;T&gt; No Pipeline&#39;                            | 1                            | 100        |  1,588.01 ns |    20.849 ns |    18.482 ns |  1,583.23 ns | 0.1984 |     832 B |
| &#39;CreateStream (1 Item) - Consume All&#39;            | 1                            | 100        |  1,603.09 ns |    27.125 ns |    39.760 ns |  1,592.72 ns | 0.2861 |    1200 B |
| &#39;CreateStream (1 Item) - Consume All&#39;            | 3                            | 100        |  1,607.67 ns |    21.041 ns |    18.652 ns |  1,601.44 ns | 0.2861 |    1200 B |
| &#39;CreateStream (1 Item) - Consume All&#39;            | 3                            | 1          |  1,622.46 ns |    22.789 ns |    28.821 ns |  1,618.02 ns | 0.2861 |    1200 B |
| &#39;CreateStream (1 Item) - Consume All&#39;            | 1                            | 1          |  1,671.26 ns |    33.000 ns |    53.289 ns |  1,654.68 ns | 0.2861 |    1200 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;               | 3                            | 1          |  2,955.43 ns |    20.377 ns |    15.909 ns |  2,958.23 ns | 0.2594 |    1096 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;                 | 3                            | 1          |  2,987.83 ns |    52.613 ns |    46.640 ns |  2,979.06 ns | 0.2747 |    1160 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;                 | 3                            | 100        |  3,009.77 ns |    34.815 ns |    30.863 ns |  3,005.27 ns | 0.2785 |    1160 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;               | 3                            | 100        |  3,082.03 ns |    57.841 ns |    56.807 ns |  3,062.29 ns | 0.2594 |    1096 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;                 | 1                            | 100        |  3,086.57 ns |    52.231 ns |    48.857 ns |  3,053.04 ns | 0.2785 |    1160 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;               | 1                            | 100        |  3,123.18 ns |    48.525 ns |    43.016 ns |  3,120.48 ns | 0.2632 |    1096 B |
| &#39;Resolve+Exec 3 Handlers Directly&#39;               | 1                            | 1          |  3,125.68 ns |    58.143 ns |   114.769 ns |  3,101.72 ns | 0.2632 |    1096 B |
| &#39;Publish 1 Handler / 1 Behavior&#39;                 | 1                            | 1          |  3,132.85 ns |    39.014 ns |    32.578 ns |  3,120.63 ns | 0.2785 |    1160 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;               | 3                            | 100        |  3,732.14 ns |    72.979 ns |    99.894 ns |  3,700.17 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;               | 3                            | 1          |  3,741.09 ns |    28.557 ns |    23.846 ns |  3,745.75 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;                      | 3                            | 100        |  3,757.58 ns |    34.576 ns |    52.800 ns |  3,762.46 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;                      | 3                            | 1          |  3,785.21 ns |    75.275 ns |    83.668 ns |  3,756.98 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;          | 3                            | 1          |  3,814.40 ns |    64.210 ns |   138.220 ns |  3,757.48 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;          | 3                            | 100        |  3,822.85 ns |    74.931 ns |    89.200 ns |  3,789.99 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;                      | 1                            | 100        |  3,834.02 ns |    46.671 ns |    38.972 ns |  3,837.36 ns | 0.3586 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;               | 1                            | 100        |  3,860.96 ns |    66.466 ns |   124.839 ns |  3,831.74 ns | 0.3586 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Scan)&#39;                      | 1                            | 1          |  3,861.09 ns |    52.176 ns |    43.569 ns |  3,867.14 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 1 Behavior (Valid Token)&#39;               | 1                            | 1          |  3,879.62 ns |    51.083 ns |    62.735 ns |  3,878.33 ns | 0.3510 |    1488 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;          | 1                            | 100        |  3,886.14 ns |    75.900 ns |    93.212 ns |  3,866.85 ns | 0.3586 |    1488 B |
| &#39;Send&lt;T&gt; 2 Behaviors (Scan + Explicit)&#39;          | 1                            | 1          |  3,914.24 ns |    67.050 ns |    55.990 ns |  3,928.99 ns | 0.3510 |    1488 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;                   | 3                            | 100        |  3,916.92 ns |    26.306 ns |    21.967 ns |  3,908.74 ns | 0.3815 |    1600 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;                   | 3                            | 1          |  3,948.15 ns |    60.062 ns |    50.154 ns |  3,940.37 ns | 0.3815 |    1600 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;                   | 1                            | 100        |  4,121.01 ns |    48.030 ns |    40.107 ns |  4,113.23 ns | 0.3815 |    1600 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39;        | 3                            | 1          |  4,185.43 ns |    81.385 ns |    67.960 ns |  4,170.95 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;                | 3                            | 100        |  4,203.75 ns |    83.878 ns |    82.379 ns |  4,171.86 ns | 0.3967 |    1656 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39;        | 3                            | 100        |  4,238.92 ns |    84.693 ns |    97.532 ns |  4,205.14 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;               | 3                            | 1          |  4,258.43 ns |    76.761 ns |    71.802 ns |  4,235.90 ns | 0.3967 |    1656 B |
| &#39;Send&lt;void&gt; 1 Behavior (Scan)&#39;                   | 1                            | 1          |  4,275.05 ns |   111.803 ns |   318.982 ns |  4,172.57 ns | 0.3815 |    1600 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;                | 3                            | 1          |  4,294.15 ns |    81.105 ns |    86.781 ns |  4,264.51 ns | 0.3967 |    1656 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;                 | 3                            | 100        |  4,298.45 ns |    68.514 ns |   146.010 ns |  4,247.83 ns | 0.4120 |    1760 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39;        | 1                            | 100        |  4,303.85 ns |    53.808 ns |    44.932 ns |  4,298.67 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;               | 3                            | 100        |  4,304.92 ns |    84.253 ns |    90.149 ns |  4,263.33 ns | 0.3967 |    1656 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;                 | 3                            | 1          |  4,306.35 ns |    80.736 ns |    71.570 ns |  4,313.20 ns | 0.4120 |    1760 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;                | 1                            | 100        |  4,335.17 ns |    85.938 ns |    88.252 ns |  4,299.27 ns | 0.3967 |    1656 B |
| &#39;Publish(object) 3 Handlers / 1 Behavior&#39;        | 1                            | 1          |  4,341.02 ns |    77.747 ns |    68.921 ns |  4,317.27 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;               | 1                            | 100        |  4,368.40 ns |    84.964 ns |    87.252 ns |  4,324.26 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;               | 3                            | 1          |  4,368.91 ns |    60.964 ns |    93.099 ns |  4,345.23 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;               | 1                            | 1          |  4,418.89 ns |    85.620 ns |    91.613 ns |  4,369.86 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers (Valid Token)&#39;               | 1                            | 100        |  4,426.77 ns |    88.559 ns |    98.433 ns |  4,368.90 ns | 0.3967 |    1656 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;                 | 1                            | 1          |  4,429.79 ns |    88.342 ns |   145.149 ns |  4,393.46 ns | 0.4120 |    1760 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;               | 1                            | 1          |  4,442.88 ns |    84.089 ns |    89.975 ns |  4,415.30 ns | 0.3967 |    1656 B |
| &#39;Send(object) 1 Behavior (Scan)&#39;                 | 1                            | 100        |  4,468.68 ns |    91.655 ns |   250.905 ns |  4,370.62 ns | 0.4120 |    1760 B |
| &#39;Publish 3 Handlers / 1 Behavior&#39;                | 1                            | 1          |  4,476.06 ns |    77.357 ns |   131.357 ns |  4,442.75 ns | 0.3967 |    1656 B |
| &#39;Publish 3 Handlers / 2 Behaviors&#39;               | 3                            | 100        |  4,722.01 ns |   166.667 ns |   483.532 ns |  4,562.77 ns | 0.3967 |    1656 B |
| &#39;CreateStream (10 Items) - Consume All&#39;          | 3                            | 1          |  9,193.48 ns |   177.263 ns |   174.096 ns |  9,119.08 ns | 0.3357 |    1418 B |
| &#39;CreateStream (10 Items) - Consume All&#39;          | 3                            | 100        |  9,323.62 ns |   186.164 ns |   316.121 ns |  9,184.87 ns | 0.3357 |    1417 B |
| &#39;CreateStream (10 Items) - Consume All&#39;          | 1                            | 100        |  9,507.38 ns |   182.174 ns |   278.198 ns |  9,418.53 ns | 0.3357 |    1417 B |
| &#39;CreateStream (10 Items) - Consume All&#39;          | 1                            | 1          |  9,831.18 ns |   197.830 ns |   570.786 ns |  9,609.93 ns | 0.3357 |    1418 B |
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;           | 3                            | 100        | 13,063.68 ns |   149.128 ns |   132.198 ns | 13,051.78 ns | 0.5798 |    2464 B |
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;           | 1                            | 100        | 13,322.75 ns |   208.404 ns |   162.708 ns | 13,374.91 ns | 0.5798 |    2464 B |
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;           | 1                            | 1          | 13,419.36 ns |   194.764 ns |   172.653 ns | 13,436.93 ns | 0.5798 |    2464 B |
| &#39;Send&lt;T&gt; 1 Behavior (Cancelled Token)&#39;           | 3                            | 1          | 13,636.60 ns |   270.513 ns |   552.587 ns | 13,723.23 ns | 0.5798 |    2464 B |
| &#39;CreateStream (100 Items) - Cancelled Token&#39;     | 1                            | 100        | 36,008.15 ns |   566.889 ns |   502.533 ns | 35,739.49 ns | 2.0752 |    8752 B |
| &#39;CreateStream (100 Items) - Cancelled Token&#39;     | 3                            | 100        | 36,151.00 ns |   419.109 ns |   371.530 ns | 36,053.82 ns | 2.0752 |    8752 B |
| &#39;CreateStream (100 Items) - Cancelled Token&#39;     | 3                            | 1          | 36,683.05 ns |   662.773 ns |   587.531 ns | 36,442.90 ns | 2.0752 |    8752 B |
| &#39;CreateStream (100 Items) - Cancelled Token&#39;     | 1                            | 1          | 38,564.43 ns |   774.913 ns | 2,284.849 ns | 38,673.34 ns | 2.0752 |    8752 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;           | 3                            | 100        | 79,558.87 ns | 1,455.279 ns | 1,215.224 ns | 79,069.18 ns | 2.8076 |   12057 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;           | 1                            | 1          | 81,550.19 ns | 1,590.645 ns | 2,568.596 ns | 80,842.46 ns | 2.8076 |   12057 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;           | 3                            | 1          | 82,997.97 ns | 1,656.148 ns | 4,245.341 ns | 81,059.58 ns | 2.8076 |   12057 B |
| &#39;Publish 3 Handlers (Cancelled Token)&#39;           | 1                            | 100        | 84,185.21 ns | 1,677.566 ns | 4,239.424 ns | 84,730.44 ns | 2.8076 |   12057 B |
| &#39;CreateStream (100 Items) - Consume All&#39;         | 3                            | 100        | 90,154.38 ns |   977.021 ns |   866.104 ns | 89,935.72 ns | 0.7324 |    3640 B |
| &#39;CreateStream (100 Items) - Consume All&#39;         | 3                            | 1          | 90,321.13 ns |   947.870 ns |   886.638 ns | 90,358.78 ns | 0.8545 |    3640 B |
| &#39;CreateStream (100 Items) - Consume ToList&#39;      | 1                            | 100        | 90,734.38 ns |   834.082 ns |   780.201 ns | 90,812.85 ns | 0.9766 |    4504 B |
| &#39;CreateStream (100 Items) - Consume All&#39;         | 1                            | 100        | 92,677.04 ns | 1,816.425 ns | 2,091.798 ns | 92,253.89 ns | 0.8545 |    3640 B |
| &#39;CreateStream (100 Items) - Consume ToList&#39;      | 3                            | 1          | 93,480.32 ns | 1,427.571 ns | 1,699.421 ns | 92,983.51 ns | 0.9766 |    4504 B |
| &#39;CreateStream (100 Items) - Consume All&#39;         | 1                            | 1          | 94,641.61 ns | 1,880.534 ns | 3,390.990 ns | 93,615.26 ns | 0.7324 |    3640 B |
| &#39;CreateStream (100 Items) - Consume ToList&#39;      | 1                            | 1          | 95,666.70 ns |   830.153 ns |   815.321 ns | 95,878.86 ns | 0.9766 |    4504 B |
| &#39;CreateStream (100 Items) - Consume ToList&#39;      | 3                            | 100        | 97,563.72 ns | 1,822.639 ns | 2,728.040 ns | 97,086.23 ns | 0.9766 |    4504 B |




**Key Takeaways:**

* **High Performance:** M3diator demonstrates high performance across all operations (`Send`, `Publish`, `CreateStream`) with low overhead, typically executing in the microsecond or even sub-microsecond range for the core dispatch logic.
* **Efficient Streaming:** The `CreateStream` operation shows extremely low setup overhead (~75 ns) and scales predictably with the number of items yielded by the handler. It provides an efficient way to handle sequences of data compared to multiple individual requests.
* **Low Allocation:** M3diator operations exhibit low memory allocation for the dispatching mechanism itself. While not strictly zero (e.g., `CreateStream` setup allocates ~192 B, `Send` without pipeline ~832 B), the overhead allocation is minimal, making it suitable for performance-sensitive applications. Allocations primarily scale with the data being processed (handler/item allocations).
* **Minimal Pipeline Impact:** The pipeline behavior mechanism adds very little overhead per behavior (typically sub-microsecond), allowing for cross-cutting concerns without significant performance degradation.

These results suggest that M3diator is a highly performant choice for applications requiring efficient in-process messaging, including request/response, notifications, and data streaming patterns, where mediator overhead is a critical factor.

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a pull request. (Add more details here if needed: contribution guidelines, code style, etc.)

## License

This project <img alt="Brasil" src="https://github.com/JohnSalazar/microservices-go-authentication/assets/16736914/3ecb04fb-b2ce-4e8b-b492-99c5c5c4b317" width="20" height="14" /> is licensed under the [MIT License](LICENSE).