# Product Requirements Document  
## Products Web API — Clean Architecture (.NET 8)

---

## 1. Executive Summary

This document defines the complete technical design and implementation plan for a production-grade **Products Web API** built on **.NET 8** using **Clean Architecture**. The API exposes an anonymous health-check endpoint and secured endpoints for product management. The system is designed to participate in an event-driven microservices ecosystem alongside Orders, Payments, and Notifications services.

---

## 2. Goals and Non-Goals

### Goals
- Production-ready .NET 8 Web API following Clean Architecture principles
- JWT-secured product management endpoints (create, list, filter by colour)
- Anonymous health-check endpoint
- Full unit and integration test coverage
- React frontend to consume the API
- Event-driven architecture design suitable for microservices

### Non-Goals
- Full OAuth2 / Identity Provider integration (mocked JWT issuer is sufficient)
- Order or Payment service implementation (architecture diagram only)
- Multi-tenancy

---

## 3. Solution Structure

```
ProductService/
├── src/
│   ├── ProductService.Domain/          # Layer 1 — innermost
│   ├── ProductService.Application/     # Layer 2
│   ├── ProductService.Infrastructure/  # Layer 3
│   └── ProductService.API/             # Layer 4 — outermost
├── tests/
│   ├── ProductService.UnitTests/
│   └── ProductService.IntegrationTests/
├── frontend/
│   └── products-ui/                    # React (Vite + TypeScript)
├── docker-compose.yml
├── ProductService.sln
└── architecture-diagram.png
```

**Dependency rule:** each layer only depends inward. API → Infrastructure → Application → Domain. Domain has zero external dependencies.

---

## 4. Layer 1 — Domain (`ProductService.Domain`)

### 4.1 Purpose
Contains enterprise-wide business rules: entities, value objects, domain events, and repository contracts. Zero NuGet dependencies (no EF Core, no MediatR).

### 4.2 Project File
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### 4.3 Entities

#### `Product` (Aggregate Root)
```
ProductService.Domain/
└── Entities/
    └── Product.cs
```

**Fields:**

| Property     | Type            | Rules                                              |
|--------------|-----------------|----------------------------------------------------|
| Id           | Guid            | Generated on creation, never reassigned            |
| Name         | string          | Required, 1–200 chars                              |
| Description  | string?         | Optional, max 1000 chars                           |
| Colour       | Colour (enum)   | Required, value from predefined enum               |
| Price        | decimal         | > 0, max 2 decimal places                          |
| StockQuantity| int             | ≥ 0                                                |
| CreatedAt    | DateTimeOffset  | Set on construction (UTC)                          |
| UpdatedAt    | DateTimeOffset  | Updated on any state change (UTC)                  |
| IsDeleted    | bool            | Soft-delete flag                                   |

**Behaviour (methods on aggregate root):**
- `static Product Create(string name, string? description, Colour colour, decimal price, int stockQuantity)` — factory method, raises `ProductCreatedDomainEvent`
- `void UpdateStock(int quantity)` — raises `ProductStockUpdatedDomainEvent`
- `void Delete()` — sets `IsDeleted = true`, raises `ProductDeletedDomainEvent`

**Domain invariants enforced inside the entity:**
```
if (price <= 0) throw new DomainException("Price must be positive.");
if (stockQuantity < 0) throw new DomainException("Stock cannot be negative.");
if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
```

#### `Colour` (Value Object / Enum)
```csharp
public enum Colour
{
    Red, Blue, Green, Yellow, Black, White, Orange, Purple, Pink, Brown, Grey
}
```

### 4.4 Domain Events

```
ProductService.Domain/
└── Events/
    ├── IDomainEvent.cs
    ├── ProductCreatedDomainEvent.cs
    ├── ProductStockUpdatedDomainEvent.cs
    └── ProductDeletedDomainEvent.cs
```

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public record ProductCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    string Name,
    Colour Colour,
    decimal Price
) : IDomainEvent;
```

### 4.5 Base Entity with Domain Events

```csharp
// ProductService.Domain/Common/AggregateRoot.cs
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 4.6 Repository Interface (port — defined in Domain)

```csharp
// ProductService.Domain/Repositories/IProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByColourAsync(Colour colour, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
```

### 4.7 Common

```
ProductService.Domain/
└── Common/
    ├── AggregateRoot.cs
    ├── DomainException.cs
    └── Result.cs          # Railway-oriented programming helper
```

```csharp
// Result.cs — avoids exception-driven flow in Application layer
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

---

## 5. Layer 2 — Application (`ProductService.Application`)

### 5.1 Purpose
Contains application-specific business rules: use cases, CQRS command/query handlers, DTOs, validation, and application service interfaces. References only `Domain`. No HTTP, no EF Core.

### 5.2 NuGet Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.x | CQRS dispatcher |
| FluentValidation | 11.x | Input validation |
| Microsoft.Extensions.Logging.Abstractions | 8.x | Logging interface |

### 5.3 Directory Structure
```
ProductService.Application/
├── Products/
│   ├── Commands/
│   │   └── CreateProduct/
│   │       ├── CreateProductCommand.cs
│   │       ├── CreateProductCommandHandler.cs
│   │       └── CreateProductCommandValidator.cs
│   ├── Queries/
│   │   ├── GetAllProducts/
│   │   │   ├── GetAllProductsQuery.cs
│   │   │   └── GetAllProductsQueryHandler.cs
│   │   └── GetProductsByColour/
│   │       ├── GetProductsByColourQuery.cs
│   │       └── GetProductsByColourQueryHandler.cs
│   └── DTOs/
│       ├── ProductDto.cs
│       └── CreateProductRequest.cs
├── Common/
│   ├── Behaviours/
│   │   ├── ValidationBehaviour.cs
│   │   ├── LoggingBehaviour.cs
│   │   └── PerformanceBehaviour.cs
│   └── Interfaces/
│       ├── IUnitOfWork.cs
│       └── IEventPublisher.cs
└── DependencyInjection.cs
```

### 5.4 Commands

#### `CreateProductCommand`
```csharp
public record CreateProductCommand(
    string Name,
    string? Description,
    string Colour,
    decimal Price,
    int StockQuantity
) : IRequest<Result<ProductDto>>;
```

#### `CreateProductCommandValidator`
```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be positive.")
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Colour)
            .NotEmpty()
            .Must(c => Enum.TryParse<Colour>(c, true, out _))
            .WithMessage("Invalid colour value.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description is not null);
    }
}
```

#### `CreateProductCommandHandler`
```csharp
public class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<CreateProductCommandHandler> logger
) : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request, CancellationToken ct)
    {
        var colour = Enum.Parse<Colour>(request.Colour, true);
        var product = Product.Create(
            request.Name, request.Description,
            colour, request.Price, request.StockQuantity);

        await repository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Publish domain events to integration event bus
        foreach (var domainEvent in product.DomainEvents)
            await eventPublisher.PublishAsync(domainEvent, ct);

        product.ClearDomainEvents();

        logger.LogInformation("Product created: {ProductId}", product.Id);

        return Result<ProductDto>.Success(ProductDto.FromEntity(product));
    }
}
```

### 5.5 Queries

#### `GetAllProductsQuery`
```csharp
public record GetAllProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public class GetAllProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetAllProductsQuery request, CancellationToken ct)
    {
        var products = await repository.GetAllAsync(ct);
        return products.Select(ProductDto.FromEntity).ToList();
    }
}
```

#### `GetProductsByColourQuery`
```csharp
public record GetProductsByColourQuery(string Colour)
    : IRequest<Result<IReadOnlyList<ProductDto>>>;

public class GetProductsByColourQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsByColourQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsByColourQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<Colour>(request.Colour, true, out var colour))
            return Result<IReadOnlyList<ProductDto>>.Failure($"'{request.Colour}' is not a valid colour.");

        var products = await repository.GetByColourAsync(colour, ct);
        return Result<IReadOnlyList<ProductDto>>.Success(
            products.Select(ProductDto.FromEntity).ToList());
    }
}
```

### 5.6 DTO

```csharp
public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Colour,
    decimal Price,
    int StockQuantity,
    DateTimeOffset CreatedAt)
{
    public static ProductDto FromEntity(Product p) => new(
        p.Id, p.Name, p.Description,
        p.Colour.ToString(), p.Price,
        p.StockQuantity, p.CreatedAt);
}
```

### 5.7 MediatR Pipeline Behaviours

#### `ValidationBehaviour`
Runs FluentValidation before every command handler. Returns a `ValidationException` (mapped to HTTP 422) if invalid.

```csharp
public class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

#### `LoggingBehaviour`
Logs request name, elapsed time, and any exceptions at the pipeline level.

#### `PerformanceBehaviour`
Logs a warning for any request exceeding 500 ms.

### 5.8 Interfaces

```csharp
// IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// IEventPublisher.cs
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
```

### 5.9 DependencyInjection Extension

```csharp
// DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
```

---

## 6. Layer 3 — Infrastructure (`ProductService.Infrastructure`)

### 6.1 Purpose
Implements the ports defined in Domain and Application: EF Core persistence, JWT auth, event bus adapter, and external service clients.

### 6.2 NuGet Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.x | EF Core SQL Server provider |
| Microsoft.EntityFrameworkCore.Design | 8.x | Migrations tooling |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.x | JWT middleware |
| MassTransit | 8.x (or Azure.Messaging.ServiceBus) | Message bus adapter |
| Serilog.AspNetCore | 8.x | Structured logging |

### 6.3 Directory Structure
```
ProductService.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   └── ProductConfiguration.cs
│   ├── Repositories/
│   │   └── ProductRepository.cs
│   ├── Migrations/
│   └── UnitOfWork.cs
├── Messaging/
│   └── EventPublisher.cs
├── Authentication/
│   └── JwtSettings.cs
└── DependencyInjection.cs
```

### 6.4 Entity Framework Core

#### `ApplicationDbContext`
```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-set UpdatedAt on modified entities
        foreach (var entry in ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
```

#### `ProductConfiguration`
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Colour)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasQueryFilter(p => !p.IsDeleted);  // global soft-delete filter

        builder.HasIndex(p => p.Colour);  // index for colour-based queries
        builder.HasIndex(p => p.CreatedAt);
    }
}
```

#### `ProductRepository`
```csharp
public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        => context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetByColourAsync(Colour colour, CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .Where(p => p.Colour == colour)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct)
        => await context.Products.AddAsync(product, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => context.Products.AnyAsync(p => p.Id == id, ct);
}
```

### 6.5 Messaging — Event Publisher

```csharp
// EventPublisher.cs — publishes domain events to the message bus
public class EventPublisher(IPublishEndpoint publishEndpoint, ILogger<EventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        logger.LogInformation("Publishing event {EventType} ({EventId})",
            domainEvent.GetType().Name, domainEvent.EventId);

        await publishEndpoint.Publish((dynamic)domainEvent, ct);
    }
}
```

### 6.6 Authentication — JWT Settings

```csharp
// JwtSettings.cs
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
}
```

JWT Bearer configuration (in `DependencyInjection.cs`):
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
```

---

## 7. Layer 4 — API (`ProductService.API`)

### 7.1 Purpose
HTTP surface: controllers, middleware, health checks, OpenAPI, and startup composition. References only `Application` and `Infrastructure` (for DI wiring).

### 7.2 NuGet Dependencies
| Package | Version | Purpose |
|---------|---------|---------|
| Swashbuckle.AspNetCore | 6.x | OpenAPI / Swagger |
| Serilog.AspNetCore | 8.x | Structured request logging |
| AspNetCore.HealthChecks.SqlServer | 8.x | DB health probe |
| Microsoft.AspNetCore.OpenApi | 8.x | OpenAPI metadata |

### 7.3 Directory Structure
```
ProductService.API/
├── Controllers/
│   ├── HealthController.cs
│   ├── ProductsController.cs
│   └── AuthController.cs          # Issues test JWTs (non-production IdP)
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── CorrelationIdMiddleware.cs
├── Extensions/
│   ├── SwaggerExtensions.cs
│   └── HealthCheckExtensions.cs
├── Models/
│   └── ApiErrorResponse.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

### 7.4 Controllers

#### `HealthController` — Anonymous
```csharp
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow });
}
```

#### `ProductsController` — Secured
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductsController(ISender mediator) : ControllerBase
{
    // POST /api/products
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : UnprocessableEntity(new ApiErrorResponse(result.Error!));
    }

    // GET /api/products
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await mediator.Send(new GetAllProductsQuery(), ct);
        return Ok(products);
    }

    // GET /api/products?colour=Red
    [HttpGet("by-colour/{colour}")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByColour(string colour, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductsByColourQuery(colour), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new ApiErrorResponse(result.Error!));
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
```

#### `AuthController` — Issues test tokens
```csharp
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("token")]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        // Hardcoded test users — replace with real IdP in production
        if (request.Username != "admin" || request.Password != "password")
            return Unauthorized();

        var token = GenerateJwt(request.Username, configuration);
        return Ok(new { token });
    }
}
```

> **Production note:** Replace `AuthController` with a redirect to a real Identity Provider (Keycloak, Azure AD B2C, Auth0, etc.).

### 7.5 Exception Handling Middleware

```csharp
public class ExceptionHandlingMiddleware(
    RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                "Validation failed",
                ex.Errors.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse("An unexpected error occurred."));
        }
    }
}
```

### 7.6 Correlation ID Middleware

Reads or generates an `X-Correlation-Id` header on every request, propagates it through logging scope and response headers.

### 7.7 `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithCorrelationId()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Clean Architecture DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(cfg =>
{
    cfg.SwaggerDoc("v1", new() { Title = "Products API", Version = "v1" });
    cfg.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
    cfg.AddSecurityRequirement(new OpenApiSecurityRequirement { ... });
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck<MessageBusHealthCheck>("message-bus");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Apply EF migrations on startup (dev/test only — use CI pipeline in prod)
if (app.Environment.IsDevelopment())
    await app.MigrateDatabase();

app.Run();
```

### 7.8 `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProductsDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Issuer": "ProductService",
    "Audience": "ProductServiceClients",
    "SecretKey": "<stored-in-secrets-manager-not-here>",
    "ExpiryMinutes": 60
  },
  "MessageBus": {
    "Host": "rabbitmq://localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning" } }
  }
}
```

---

## 8. API Contract

### Base URL
`https://api.products.example.com/api`

### Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | None | Health check |
| POST | `/auth/token` | None | Obtain JWT (test only) |
| POST | `/products` | JWT Bearer | Create a product |
| GET | `/products` | JWT Bearer | List all products |
| GET | `/products/{id}` | JWT Bearer | Get single product |
| GET | `/products/by-colour/{colour}` | JWT Bearer | Filter by colour |

### Request: Create Product
```json
POST /api/products
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Classic Red Shirt",
  "description": "A timeless red shirt.",
  "colour": "Red",
  "price": 29.99,
  "stockQuantity": 150
}
```

### Response: 201 Created
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Classic Red Shirt",
  "description": "A timeless red shirt.",
  "colour": "Red",
  "price": 29.99,
  "stockQuantity": 150,
  "createdAt": "2026-09-30T10:00:00Z"
}
```

### Response: 422 Validation Error
```json
{
  "title": "Validation failed",
  "errors": ["Price must be positive.", "Name is required."]
}
```

---

## 9. Testing Strategy

### 9.1 Unit Tests (`ProductService.UnitTests`)

**NuGet:**
- xUnit 2.x
- FluentAssertions 6.x
- Moq 4.x / NSubstitute 5.x
- Bogus 35.x (test data generation)

#### Domain Tests
```
UnitTests/
└── Domain/
    ├── ProductTests.cs          # Entity creation, invariants, domain events
    └── ColourTests.cs           # Enum parsing edge cases
```

**Example:**
```csharp
[Fact]
public void Create_WithNegativePrice_ThrowsDomainException()
{
    // Act
    var act = () => Product.Create("Shirt", null, Colour.Red, -1m, 10);

    // Assert
    act.Should().Throw<DomainException>()
       .WithMessage("Price must be positive.");
}

[Fact]
public void Create_ValidProduct_RaisesProductCreatedDomainEvent()
{
    var product = Product.Create("Shirt", null, Colour.Red, 29.99m, 100);

    product.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<ProductCreatedDomainEvent>();
}
```

#### Application Tests
```
UnitTests/
└── Application/
    ├── CreateProductCommandHandlerTests.cs
    ├── GetAllProductsQueryHandlerTests.cs
    ├── GetProductsByColourQueryHandlerTests.cs
    └── Validators/
        └── CreateProductCommandValidatorTests.cs
```

**Example — Handler with mocked repository:**
```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccessResultWithProductDto()
{
    var repositoryMock = Substitute.For<IProductRepository>();
    var unitOfWorkMock = Substitute.For<IUnitOfWork>();
    var publisherMock  = Substitute.For<IEventPublisher>();
    var loggerMock     = Substitute.For<ILogger<CreateProductCommandHandler>>();

    var handler = new CreateProductCommandHandler(
        repositoryMock, unitOfWorkMock, publisherMock, loggerMock);

    var command = new CreateProductCommand("Shirt", null, "Red", 29.99m, 100);
    var result  = await handler.Handle(command, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value!.Name.Should().Be("Shirt");
    await unitOfWorkMock.Received(1).SaveChangesAsync(CancellationToken.None);
}
```

### 9.2 Integration Tests (`ProductService.IntegrationTests`)

**NuGet:**
- Microsoft.AspNetCore.Mvc.Testing 8.x
- Testcontainers.MsSql 3.x (spins up SQL Server in Docker)
- WireMock.Net (mock external services)

```
IntegrationTests/
├── ProductsApiTests.cs       # End-to-end HTTP tests against real DB
├── HealthCheckTests.cs
├── Fixtures/
│   ├── WebApplicationFactory.cs
│   └── DatabaseFixture.cs     # Testcontainers SQL Server
└── Helpers/
    └── JwtTokenHelper.cs      # Generates valid test JWTs
```

**Example — Full HTTP round-trip:**
```csharp
public class ProductsApiTests(WebAppFactory factory)
    : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task CreateProduct_ValidRequest_Returns201WithLocation()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Test Product",
            colour = "Blue",
            price = 49.99,
            stockQuantity = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        body!.Name.Should().Be("Test Product");
        body.Colour.Should().Be("Blue");
    }

    [Fact]
    public async Task GetAllProducts_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByColour_WithValidColour_ReturnsFilteredList()
    {
        var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products",
            new { name = "Red Shirt", colour = "Red", price = 20m, stockQuantity = 10 });
        await client.PostAsJsonAsync("/api/products",
            new { name = "Blue Jeans", colour = "Blue", price = 40m, stockQuantity = 5 });

        var response = await client.GetAsync("/api/products/by-colour/Red");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        products!.Should().AllSatisfy(p => p.Colour.Should().Be("Red"));
    }
}
```

---

## 10. React Frontend (`products-ui`)

### 10.1 Stack
- React 18 + TypeScript + Vite
- TanStack Query (data fetching / caching)
- React Hook Form + Zod (form validation)
- Axios (HTTP client with JWT interceptor)
- Shadcn/ui + Tailwind CSS

### 10.2 Directory Structure
```
products-ui/
├── src/
│   ├── api/
│   │   ├── client.ts            # Axios instance with auth interceptor
│   │   └── products.ts          # API calls: createProduct, getProducts, getByColour
│   ├── components/
│   │   ├── ProductTable.tsx
│   │   ├── CreateProductForm.tsx
│   │   └── ColourFilter.tsx
│   ├── hooks/
│   │   ├── useProducts.ts       # TanStack Query hooks
│   │   └── useAuth.ts
│   ├── pages/
│   │   ├── LoginPage.tsx
│   │   └── ProductsPage.tsx
│   ├── store/
│   │   └── authStore.ts         # Zustand — stores JWT
│   └── App.tsx
└── vite.config.ts
```

### 10.3 Key Patterns

**Axios instance with JWT interceptor:**
```typescript
const apiClient = axios.create({ baseURL: import.meta.env.VITE_API_URL });

apiClient.interceptors.request.use(config => {
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

apiClient.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) useAuthStore.getState().logout();
    return Promise.reject(err);
  }
);
```

**TanStack Query hook:**
```typescript
export const useProducts = () =>
  useQuery({ queryKey: ['products'], queryFn: () => getProducts() });

export const useProductsByColour = (colour: string) =>
  useQuery({
    queryKey: ['products', 'colour', colour],
    queryFn: () => getProductsByColour(colour),
    enabled: !!colour
  });

export const useCreateProduct = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createProduct,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] })
  });
};
```

---

## 11. Docker & Local Development

### `docker-compose.yml`
```yaml
version: "3.9"
services:
  api:
    build: ./src/ProductService.API
    ports: ["5000:80"]
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=ProductsDb;User=sa;Password=YourStrong@Pass
      - Jwt__SecretKey=super-secret-key-at-least-256-bits
      - MessageBus__Host=rabbitmq://rabbitmq
    depends_on: [db, rabbitmq]

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Pass"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]

  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]

  frontend:
    build: ./frontend/products-ui
    ports: ["3000:3000"]
    environment:
      - VITE_API_URL=http://localhost:5000/api
```

---

## 12. Microservices Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         API Gateway / BFF                            │
│              (rate limiting, auth, routing, SSL termination)         │
└────────┬──────────────┬────────────────────────┬────────────────────┘
         │              │                        │
         ▼              ▼                        ▼
┌─────────────┐  ┌─────────────┐        ┌──────────────┐
│  Products   │  │   Orders    │        │   Payments   │
│   Service   │  │   Service   │        │   Service    │
│  (this app) │  │             │        │              │
└──────┬──────┘  └──────┬──────┘        └──────┬───────┘
       │                │                      │
       │  ProductCreated│OrderPlaced           │PaymentCompleted
       │◄───────────────┤ OrderCancelled       │PaymentFailed
       │                │◄─────────────────────┤
       │                │                      │
       └────────┬───────┴──────────────────────┘
                │
                ▼
     ┌────────────────────┐
     │   Message Broker   │   (RabbitMQ / Azure Service Bus / Kafka)
     │                    │
     └────────┬───────────┘
              │
              ▼
     ┌────────────────────┐
     │  Notifications Svc │   (email/SMS/push on order/payment events)
     └────────────────────┘

Infrastructure:
  ┌─────────────────────────────────────────────────┐
  │  Identity Provider (Keycloak / Azure AD B2C)    │
  │  Distributed Cache (Redis)                      │
  │  Centralised Logging (Seq / ELK / Azure Monitor)│
  │  Distributed Tracing (OpenTelemetry + Jaeger)   │
  └─────────────────────────────────────────────────┘
```

**Event flow example — Order placed:**
1. Customer places order → `OrderService` publishes `OrderPlaced` event
2. `ProductService` subscribes, decrements stock, publishes `ProductStockUpdated`
3. `PaymentsService` subscribes to `OrderPlaced`, charges card, publishes `PaymentCompleted`
4. `NotificationsService` subscribes to `PaymentCompleted`, sends confirmation email

---

## 13. Production-Grade Concerns

| Concern | Implementation |
|---------|---------------|
| **Structured Logging** | Serilog with JSON formatter, enriched with CorrelationId, UserId, Environment |
| **Distributed Tracing** | OpenTelemetry SDK → Jaeger / Azure Monitor |
| **Health Checks** | DB + message bus probes at `/health` (liveness) and `/health/ready` (readiness) |
| **API Versioning** | `Asp.Versioning.Mvc` — URL-segment strategy (`/api/v1/products`) |
| **Rate Limiting** | `Microsoft.AspNetCore.RateLimiting` — fixed window per IP |
| **Idempotency** | Idempotency key header on POST; deduplicate by key in DB |
| **Secrets** | `IConfiguration` reads from environment variables / Azure Key Vault; no secrets in `appsettings.json` |
| **Database Migrations** | Executed by CI/CD pipeline (`dotnet ef database update`), not at runtime in production |
| **Soft Delete** | `IsDeleted` flag + EF global query filter — data never hard-deleted |
| **Cancellation Tokens** | Passed through all async call chains to support request cancellation |
| **CORS** | Explicit allow-list of frontend origins; no wildcard in production |
| **Problem Details** | `RFC 7807` compliant error responses via `ValidationProblemDetails` |

---

## 14. NuGet Package Summary

| Project | Package | Rationale |
|---------|---------|-----------|
| Application | MediatR 12.x | CQRS dispatcher |
| Application | FluentValidation 11.x | Validation pipeline |
| Infrastructure | EF Core 8.x + SQL Server | ORM + persistence |
| Infrastructure | MassTransit 8.x | Message bus abstraction |
| Infrastructure | Serilog 8.x | Structured logging |
| API | Swashbuckle 6.x | OpenAPI docs |
| API | AspNetCore.HealthChecks.* | Health probes |
| Tests | xUnit 2.x + FluentAssertions | Test framework |
| Tests | Testcontainers.MsSql 3.x | Real DB in tests |
| Tests | NSubstitute 5.x | Mocking |

---

## 15. Acceptance Criteria Checklist

- [ ] `GET /health` returns `200 OK` with no auth token
- [ ] `GET /api/products` returns `401` without valid JWT
- [ ] `POST /api/products` with valid body + JWT returns `201` with `Location` header
- [ ] `GET /api/products` returns all non-deleted products as JSON array
- [ ] `GET /api/products/by-colour/Red` returns only Red products
- [ ] `GET /api/products/by-colour/Chartreuse` returns `400` with descriptive error
- [ ] `POST /api/products` with negative price returns `422` with field-level errors
- [ ] All unit tests pass (`dotnet test --filter Category=Unit`)
- [ ] All integration tests pass against Testcontainers SQL Server (`dotnet test --filter Category=Integration`)
- [ ] React UI can log in, view product list, filter by colour, and create a product
- [ ] Architecture diagram included in repo root
- [ ] `docker-compose up` spins up the full stack locally
