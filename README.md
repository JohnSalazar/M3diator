# M3diator

[![NuGet Version](https://img.shields.io/nuget/v/M3diator?label=M3diator&logo=nuget)](https://www.nuget.org/packages/M3diator/)
[![NuGet Version](https://img.shields.io/nuget/v/M3diator.DependencyInjection?label=M3diator.DI&logo=nuget)](https://www.nuget.org/packages/M3diator.DependencyInjection/)
M3diator is a custom implementation of the Mediator pattern for .NET applications. It draws inspiration from the popular MediatR library's API structure, providing a familiar way to decouple in-process messaging (commands, queries, notifications) without depending on the MediatR NuGet package itself.

The primary goal is to offer similar concepts like requests, handlers, notifications, and pipeline behaviors for managing cross-cutting concerns, allowing developers who are familiar with MediatR's approach to use a custom, dependency-free alternative.

## Features

* **Request/Response (Command/Query) Messaging:** Send requests and receive responses via `IMediator.Send`.
* **Notification Broadcasting:** Publish notifications to multiple handlers via `IMediator.Publish`.
* **Pipeline Behaviors:** Intercept requests and notifications to add cross-cutting concerns like logging, validation, caching, transactions, etc., using `IPipelineBehavior`.
* **Dependency Injection Friendly:** Easily register handlers and the mediator itself using the provided `M3diator.DependencyInjection` package (`AddM3diator` extension method).

## Installation

Install the necessary packages using the .NET CLI:

```bash
# Core M3diator library (interfaces and implementation)
dotnet add package M3diator
````

### Registering with `IServiceCollection`

M3diator supports `Microsoft.Extensions.DependencyInjection.Abstractions` directly. To register various M3diator services and handlers:

```
services.AddM3diator(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
```

or with an assembly:

```
services.AddM3diator(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));
```