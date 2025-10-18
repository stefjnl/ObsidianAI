# exception-handling Style Guide

## Overview
Centralized exception handling implemented as middleware following Clean Architecture, SOLID, and DRY principles. All unhandled exceptions are caught, logged, and transformed into RFC 9110 compliant ProblemDetails responses.

## File Organization

### Domain Layer (`ObsidianAI.Domain/Exceptions/`)
- `DomainException.cs` - Abstract base class with `ErrorCode` property
- `ValidationException.cs` - Input validation failures (400 Bad Request)
- `NotFoundException.cs` - Resource not found (404 Not Found)
- `ConflictException.cs` - State conflicts, duplicates (409 Conflict)
- `ExternalServiceException.cs` - External service failures (502 Bad Gateway)

### Infrastructure Layer (`ObsidianAI.Infrastructure/Middleware/`)
- `GlobalExceptionHandlingMiddleware.cs` - Main middleware implementation
- `ExceptionHandlingMiddlewareExtensions.cs` - `UseGlobalExceptionHandler()` extension method

## Naming Conventions

### Exception Classes
- **Pattern:** `{Concept}Exception` (e.g., `ValidationException`, `NotFoundException`)
- **Inheritance:** All inherit from `DomainException` abstract base class
- **Sealed:** Leaf exception classes marked `sealed` to prevent further inheritance
- **Error Codes:** Use SCREAMING_SNAKE_CASE (e.g., `VALIDATION_ERROR`, `RESOURCE_NOT_FOUND`)

### Middleware
- **Pattern:** `Global{Concern}Middleware` for cross-cutting concerns
- **Extension Method:** `Use{Middleware}` (e.g., `UseGlobalExceptionHandler()`)

## Code Structure

### Exception Constructor Patterns

**Base Exception:**
```csharp
protected DomainException(string message, string errorCode) : base(message)
protected DomainException(string message, string errorCode, Exception inner) : base(message, inner)
```

**Validation Exception:**
```csharp
public ValidationException(IDictionary<string, string[]> errors) // Multiple fields
public ValidationException(string field, string message)         // Single field
```

**Not Found Exception:**
```csharp
public NotFoundException(string resourceName, object resourceId)
public NotFoundException(string message)
```

**Service Exception:**
```csharp
public ExternalServiceException(string serviceName, string message)
public ExternalServiceException(string serviceName, string message, Exception inner)
```

### Middleware Pattern

```csharp
public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

## Exception Mapping

| Exception Type | HTTP Status | Log Level | Response Type |
|----------------|-------------|-----------|---------------|
| `ValidationException` | 400 | Warning | ValidationProblemDetails |
| `NotFoundException` | 404 | Information | ProblemDetails |
| `ConflictException` | 409 | Warning | ProblemDetails |
| `ExternalServiceException` | 502 | Error | ProblemDetails |
| `InvalidOperationException` | 400 | Warning | ProblemDetails |
| `ArgumentException` | 400 | Warning | ProblemDetails |
| All other exceptions | 500 | Critical | ProblemDetails |

## Usage Guidelines

### ✅ DO

**Throw specific exceptions:**
```csharp
public async Task<Conversation> GetAsync(Guid id)
{
    var conversation = await _repo.GetByIdAsync(id);
    if (conversation == null)
    {
        throw new NotFoundException("Conversation", id); // ✅ Middleware handles logging & response
    }
    return conversation;
}
```

**Validate early:**
```csharp
public async Task UpdateAsync(string path, string content)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        throw new ValidationException("Path", "Path cannot be empty");
    }
    
    // Business logic
}
```

**Wrap external service calls:**
```csharp
try
{
    return await _httpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    throw new ExternalServiceException("RemoteAPI", "Connection failed", ex);
}
```

### ❌ DON'T

**Avoid broad catches:**
```csharp
// ❌ BAD: Broad catch + redundant logging
try
{
    await DoWorkAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error"); // Middleware logs this too!
    throw;
}

// ✅ GOOD: Let middleware handle it
await DoWorkAsync();
```

**Avoid swallowing exceptions:**
```csharp
// ❌ BAD: Returns null instead of throwing
var item = await _repo.FindAsync(id);
if (item == null) return null;

// ✅ GOOD: Throws specific exception
if (item == null) throw new NotFoundException("Item", id);
```

**Avoid error objects:**
```csharp
// ❌ BAD: Result wrapper pattern
public async Task<Result<T>> GetAsync()
{
    try { /* ... */ }
    catch (Exception ex)
    {
        return Result<T>.Failure(ex.Message);
    }
}

// ✅ GOOD: Throw and let middleware format response
public async Task<T> GetAsync()
{
    // Just throw - middleware converts to proper API response
}
```

## ProblemDetails Response Format

### Standard Error (400/409/502)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "File path cannot be empty",
  "instance": "/api/vault/read",
  "errorCode": "VALIDATION_ERROR",
  "traceId": "0HMVFE6H3NVRQ:00000001",
  "timestamp": "2025-10-18T14:30:00Z"
}
```

### Validation Error (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred",
  "status": 400,
  "errors": {
    "Email": ["Email is required", "Email must be valid"],
    "Name": ["Name is required"]
  },
  "errorCode": "VALIDATION_ERROR",
  "traceId": "0HMVFE6H3NVRQ:00000001",
  "timestamp": "2025-10-18T14:30:00Z"
}
```

### Development Only Extensions
```json
{
  // ... standard fields ...
  "stackTrace": "   at ObsidianAI.Application.UseCases...",
  "exceptionType": "NotFoundException",
  "innerException": {
    "message": "Database connection timeout",
    "type": "SqlException"
  }
}
```

## Middleware Registration

### API Project (`Program.cs`)
```csharp
using ObsidianAI.Infrastructure.Middleware;

var app = builder.Build();

// ⚠️ Register FIRST to catch ALL exceptions
app.UseGlobalExceptionHandler();

// Then other middleware
app.MapDefaultEndpoints();
app.MapObsidianEndpoints();
```

### Web Project (`Program.cs`)
```csharp
var app = builder.Build();

// Blazor uses framework exception handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
else
{
    app.UseDeveloperExceptionPage();
}
```

## Testing

### Unit Test Exceptions
```csharp
[Fact]
public async Task WhenNotFound_ThrowsNotFoundException()
{
    // Arrange
    _mockRepo.Setup(r => r.GetAsync(It.IsAny<Guid>()))
             .ReturnsAsync((Conversation)null);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(
        () => _service.GetAsync(Guid.NewGuid())
    );
    
    Assert.Equal("RESOURCE_NOT_FOUND", ex.ErrorCode);
}
```

### Integration Test Middleware
```csharp
[Fact]
public async Task WhenValidationFails_Returns400ProblemDetails()
{
    // Arrange
    var response = await _client.PostAsJsonAsync("/api/users", new { Email = "" });
    
    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
    Assert.Equal("VALIDATION_ERROR", problem.Extensions["errorCode"]);
}
```

## Dependencies

### Infrastructure Project
```xml
<PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.2.0" />
<PackageReference Include="Microsoft.AspNetCore.Http.Extensions" Version="2.2.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.2.5" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="9.0.5" />
```

### Required Usings
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Exceptions;
using System.Net;
using System.Text.Json;
```

## Evolution Guidelines

### Adding New Exception Types

1. **Create in Domain layer** (`Domain/Exceptions/`)
2. **Inherit from `DomainException`**
3. **Define unique error code**
4. **Add to middleware mapping** (if custom status code needed)
5. **Document in exception-handling-architecture.md**

Example:
```csharp
public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message)
        : base(message, "UNAUTHORIZED")
    {
    }
}

// Add to middleware:
UnauthorizedException unauthorizedEx => (
    HttpStatusCode.Unauthorized,
    CreateProblemDetails(context, unauthorizedEx, HttpStatusCode.Unauthorized)
)
```

### Future Enhancements

- **Retry Logic:** Automatic retry for transient `ExternalServiceException`
- **Circuit Breaker:** Fail fast after repeated service failures
- **Metrics:** Track exception rates by type (Prometheus/OpenTelemetry)
- **Localization:** Multi-language error messages
- **Custom Headers:** Add `X-Error-Code` for easier client parsing

## Related Documentation

- [Exception Handling Architecture](../docs/exception-handling-architecture.md) - Comprehensive guide
- [API Configuration](./api-config-json.md) - API settings
- [Infrastructure DI](./infrastructure-di.md) - Dependency injection
- [Code Review Findings](../docs/code-review-2025-10-18.md) - Issue #2 resolution

## Key Principles

1. **Separation of Concerns:** Domain defines exceptions, Infrastructure handles them
2. **DRY:** Single middleware eliminates duplicated try-catch-log-throw patterns
3. **Open/Closed:** Easy to extend with new exception types without modifying middleware
4. **Fail Fast:** Throw early, let middleware handle gracefully
5. **Consistent API:** All errors follow RFC 9110 ProblemDetails format
6. **Context Preservation:** Inner exceptions and stack traces captured in development
7. **Correlation:** TraceId in every response for distributed tracing
