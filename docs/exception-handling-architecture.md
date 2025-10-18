# Exception Handling Architecture

**Project:** ObsidianAI  
**Created:** October 18, 2025  
**Issue:** #2 - Broad exception catching without proper logging

## Overview

This document describes the centralized exception handling architecture implemented as middleware, following Clean Architecture, SOLID, and DRY principles.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Custom Exception Types](#custom-exception-types)
3. [Middleware Implementation](#middleware-implementation)
4. [Usage Guidelines](#usage-guidelines)
5. [Migration Guide](#migration-guide)
6. [Testing Considerations](#testing-considerations)

---

## Architecture Overview

### Design Principles

The exception handling system follows these key principles:

1. **Single Responsibility (SOLID):** Each exception type has a specific purpose
2. **Open/Closed (SOLID):** Easy to extend with new exception types without modifying middleware
3. **Liskov Substitution (SOLID):** All domain exceptions inherit from `DomainException`
4. **Dependency Inversion (SOLID):** Middleware depends on abstractions (ILogger, IHostEnvironment)
5. **Don't Repeat Yourself (DRY):** Centralized exception handling eliminates repeated try-catch blocks
6. **Clean Architecture:** Domain exceptions live in Domain layer, infrastructure concerns in Infrastructure layer

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│ API / Web Layer                                             │
│ • Registers middleware in Program.cs                        │
│ • Throws specific exceptions instead of catching            │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Infrastructure Layer                                        │
│ • GlobalExceptionHandlingMiddleware                         │
│ • Catches all exceptions and transforms to ProblemDetails   │
│ • Logs with appropriate severity                            │
│ • Returns structured error responses                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Application Layer                                           │
│ • Use cases throw specific exceptions                       │
│ • No exception handling - let middleware handle it          │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Domain Layer                                                │
│ • Custom exception types (DomainException, etc.)            │
│ • Domain invariant validation throws exceptions             │
│ • No logging dependencies                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Custom Exception Types

All exception types are located in `ObsidianAI.Domain/Exceptions/`.

### Base Exception

**`DomainException`** - Abstract base class for all domain-specific exceptions

```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    
    protected DomainException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

**Properties:**
- `ErrorCode`: Machine-readable error identifier included in API responses

### Specific Exceptions

#### 1. ValidationException

**Purpose:** Validation errors for user input or business rules

**Error Code:** `VALIDATION_ERROR`

**HTTP Status:** 400 Bad Request

**Usage:**
```csharp
// Single field validation
throw new ValidationException("Email", "Email address is invalid");

// Multiple fields
var errors = new Dictionary<string, string[]>
{
    { "Email", new[] { "Email is required", "Email must be valid" } },
    { "Name", new[] { "Name is required" } }
};
throw new ValidationException(errors);
```

**Response Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/conversations",
  "errors": {
    "Email": ["Email is required", "Email must be valid"],
    "Name": ["Name is required"]
  },
  "errorCode": "VALIDATION_ERROR",
  "traceId": "0HMVFE6H3NVRQ:00000001",
  "timestamp": "2025-10-18T14:30:00Z"
}
```

#### 2. NotFoundException

**Purpose:** Resource not found errors

**Error Code:** `RESOURCE_NOT_FOUND`

**HTTP Status:** 404 Not Found

**Usage:**
```csharp
// With resource details
throw new NotFoundException("Conversation", conversationId);

// Custom message
throw new NotFoundException("File 'example.md' does not exist in vault");
```

**Response Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Conversation with identifier '12345' was not found.",
  "instance": "/api/conversations/12345",
  "errorCode": "RESOURCE_NOT_FOUND",
  "traceId": "0HMVFE6H3NVRQ:00000002",
  "timestamp": "2025-10-18T14:31:00Z"
}
```

#### 3. ConflictException

**Purpose:** State conflicts (duplicate resources, concurrent modifications)

**Error Code:** `CONFLICT`

**HTTP Status:** 409 Conflict

**Usage:**
```csharp
throw new ConflictException("A conversation with this title already exists");

// With inner exception
throw new ConflictException("Database concurrency conflict", dbUpdateEx);
```

**Response Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "A conversation with this title already exists",
  "instance": "/api/conversations",
  "errorCode": "CONFLICT",
  "traceId": "0HMVFE6H3NVRQ:00000003",
  "timestamp": "2025-10-18T14:32:00Z"
}
```

#### 4. ExternalServiceException

**Purpose:** External service failures (LLM, MCP, OpenRouter, etc.)

**Error Code:** `EXTERNAL_SERVICE_ERROR`

**HTTP Status:** 502 Bad Gateway

**Usage:**
```csharp
throw new ExternalServiceException("OpenRouter", "API quota exceeded");

// With inner exception
catch (HttpRequestException ex)
{
    throw new ExternalServiceException("MCP Gateway", "Connection timeout", ex);
}
```

**Response Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.3",
  "title": "External Service Error",
  "status": 502,
  "detail": "OpenRouter: API quota exceeded",
  "instance": "/api/chat/stream",
  "errorCode": "EXTERNAL_SERVICE_ERROR",
  "traceId": "0HMVFE6H3NVRQ:00000004",
  "timestamp": "2025-10-18T14:33:00Z"
}
```

---

## Middleware Implementation

### GlobalExceptionHandlingMiddleware

**Location:** `ObsidianAI.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs`

**Features:**
1. ✅ Catches all unhandled exceptions
2. ✅ Maps exceptions to appropriate HTTP status codes
3. ✅ Returns RFC 9110 compliant ProblemDetails responses
4. ✅ Logs exceptions with appropriate severity
5. ✅ Includes stack traces in development
6. ✅ Protects sensitive information in production
7. ✅ Adds trace IDs for request correlation
8. ✅ Supports ValidationProblemDetails for validation errors

### Exception Mapping

| Exception Type | HTTP Status | Log Level | Response Type |
|----------------|-------------|-----------|---------------|
| `ValidationException` | 400 | Warning | ValidationProblemDetails |
| `NotFoundException` | 404 | Information | ProblemDetails |
| `ConflictException` | 409 | Warning | ProblemDetails |
| `ExternalServiceException` | 502 | Error | ProblemDetails |
| `InvalidOperationException` | 400 | Warning | ProblemDetails |
| `ArgumentException` | 400 | Warning | ProblemDetails |
| All other exceptions | 500 | Critical | ProblemDetails |

### Middleware Registration

**API Project (`ObsidianAI.Api/Program.cs`):**
```csharp
using ObsidianAI.Infrastructure.Middleware;

var app = builder.Build();

// Register FIRST to catch all exceptions
app.UseGlobalExceptionHandler();

// Then other middleware
app.MapDefaultEndpoints();
app.MapObsidianEndpoints();
```

**Web Project (`ObsidianAI.Web/Program.cs`):**
```csharp
var app = builder.Build();

// For Blazor, use framework exception handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
else
{
    app.UseDeveloperExceptionPage();
}
```

**Note:** Web project uses Blazor's built-in `UseExceptionHandler` for interactive components. For Web APIs within the Web project, you could add `UseGlobalExceptionHandler()` before `UseExceptionHandler()`.

---

## Usage Guidelines

### ✅ DO: Throw Specific Exceptions

**Before:**
```csharp
public async Task<Conversation> GetConversationAsync(Guid id)
{
    try
    {
        var conversation = await _repository.GetByIdAsync(id);
        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found: {Id}", id);
            return null; // ❌ Bad: Swallowing error
        }
        return conversation;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting conversation"); // ❌ Bad: Broad catch
        throw; // ❌ Bad: Losing context
    }
}
```

**After:**
```csharp
public async Task<Conversation> GetConversationAsync(Guid id)
{
    // ✅ Good: Let repository throw, or throw specific exception
    var conversation = await _repository.GetByIdAsync(id);
    
    if (conversation == null)
    {
        throw new NotFoundException("Conversation", id); // ✅ Middleware logs & handles
    }
    
    return conversation;
}
```

### ✅ DO: Validate Input Early

```csharp
public async Task<string> ReadFileAsync(string filePath, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(filePath))
    {
        throw new ValidationException("FilePath", "File path cannot be empty");
    }
    
    // Business logic...
}
```

### ✅ DO: Wrap External Service Failures

```csharp
public async Task<ChatResponse> SendToLlmAsync(string prompt)
{
    try
    {
        return await _openRouterClient.CompleteChatAsync(prompt);
    }
    catch (HttpRequestException ex)
    {
        throw new ExternalServiceException("OpenRouter", "Failed to connect", ex);
    }
    catch (TaskCanceledException ex)
    {
        throw new ExternalServiceException("OpenRouter", "Request timeout", ex);
    }
}
```

### ❌ DON'T: Catch and Log Generic Exceptions

**Avoid:**
```csharp
try
{
    // Business logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Something went wrong");
    throw; // Middleware will log again - duplication
}
```

**Instead:**
```csharp
// Just let exceptions bubble up - middleware handles logging
var result = await _service.DoSomethingAsync();
```

### ❌ DON'T: Return Error Objects

**Avoid:**
```csharp
public class Result
{
    public bool Success { get; set; }
    public string Error { get; set; }
}

public async Task<Result> DoSomething()
{
    try
    {
        // ...
        return new Result { Success = true };
    }
    catch (Exception ex)
    {
        return new Result { Success = false, Error = ex.Message };
    }
}
```

**Instead:**
```csharp
public async Task DoSomething()
{
    // Throw exceptions - middleware converts to proper API response
    if (somethingWrong)
    {
        throw new ValidationException("Field", "Error message");
    }
}
```

---

## Migration Guide

### Step 1: Identify Broad Catches

Search codebase for:
```
catch\s*\(\s*Exception
```

### Step 2: Analyze Each Catch Block

For each `catch (Exception ex)` block, determine:

1. **Is it validation?** → Throw `ValidationException`
2. **Is it "not found"?** → Throw `NotFoundException`
3. **Is it external service?** → Throw `ExternalServiceException`
4. **Is it a conflict?** → Throw `ConflictException`
5. **Is it truly unexpected?** → Remove try-catch, let middleware handle

### Step 3: Replace Patterns

#### Pattern 1: Validation Errors

**Before:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Invalid input");
    throw new InvalidOperationException("Invalid input", ex);
}
```

**After:**
```csharp
// Don't catch - validate before operation
if (string.IsNullOrEmpty(input))
{
    throw new ValidationException("Input", "Input cannot be empty");
}
```

#### Pattern 2: Not Found

**Before:**
```csharp
var item = await _repo.FindAsync(id);
if (item == null)
{
    _logger.LogWarning("Item not found: {Id}", id);
    return null;
}
```

**After:**
```csharp
var item = await _repo.FindAsync(id);
if (item == null)
{
    throw new NotFoundException("Item", id);
}
```

#### Pattern 3: External Services

**Before:**
```csharp
try
{
    return await _httpClient.GetAsync(url);
}
catch (Exception ex)
{
    _logger.LogError(ex, "HTTP request failed");
    throw;
}
```

**After:**
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

#### Pattern 4: Remove Redundant Logging

**Before:**
```csharp
try
{
    await DoWorkAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to do work");
    throw; // Middleware also logs - duplication!
}
```

**After:**
```csharp
// Just remove the try-catch - middleware logs everything
await DoWorkAsync();
```

---

## Testing Considerations

### Unit Testing Exceptions

```csharp
[Fact]
public async Task GetConversation_WhenNotFound_ThrowsNotFoundException()
{
    // Arrange
    var service = new ConversationService(_mockRepo.Object);
    
    _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
             .ReturnsAsync((Conversation)null);
    
    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(
        () => service.GetConversationAsync(Guid.NewGuid())
    );
    
    Assert.Equal("RESOURCE_NOT_FOUND", exception.ErrorCode);
}
```

### Integration Testing Middleware

```csharp
[Fact]
public async Task Api_WhenValidationFails_Returns400WithProblemDetails()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new { Email = "" }; // Invalid
    
    // Act
    var response = await client.PostAsJsonAsync("/api/users", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
    Assert.NotNull(problem);
    Assert.Equal("VALIDATION_ERROR", problem.Extensions["errorCode"]);
    Assert.Contains("Email", problem.Errors.Keys);
}
```

### Testing SignalR Hub Exceptions

```csharp
[Fact]
public async Task ChatHub_WhenExceptionThrown_ClientReceivesError()
{
    // SignalR has built-in exception handling
    // Exceptions in hub methods return error to client automatically
    var connection = new HubConnectionBuilder()
        .WithUrl($"http://localhost/chathub")
        .Build();
    
    await connection.StartAsync();
    
    var exception = await Assert.ThrowsAsync<HubException>(
        () => connection.InvokeAsync("SendMessage", "")
    );
    
    Assert.Contains("validation", exception.Message, StringComparison.OrdinalIgnoreCase);
}
```

---

## Logging Strategy

### Log Levels by Exception Type

| Exception | Log Level | Rationale |
|-----------|-----------|-----------|
| `ValidationException` | Warning | User input error, not system failure |
| `NotFoundException` | Information | Expected condition, not an error |
| `ConflictException` | Warning | User action issue, not system failure |
| `ExternalServiceException` | Error | System dependency failure |
| `DomainException` | Warning | Business rule violation |
| Other exceptions | Critical | Unexpected system failure |

### Log Enrichment

The middleware automatically adds:
- **Exception type name** (`exceptionType`)
- **Stack trace** (development only)
- **Inner exception details** (development only)
- **Trace ID** (always)
- **Timestamp** (always)

### Example Log Output

**Production (Error Level):**
```
2025-10-18 14:35:22.134 [ERR] Unhandled exception: ExternalServiceException - OpenRouter: API quota exceeded
TraceId: 0HMVFE6H3NVRQ:00000004
Path: /api/chat/stream
```

**Development (Error Level with stack trace):**
```
2025-10-18 14:35:22.134 [ERR] Unhandled exception: ExternalServiceException - OpenRouter: API quota exceeded
   at ObsidianAI.Infrastructure.LLM.OpenRouterClientFactory.SendRequestAsync(String prompt)
   at ObsidianAI.Application.UseCases.StreamChatUseCase.ExecuteAsync(ChatRequest request, CancellationToken ct)
TraceId: 0HMVFE6H3NVRQ:00000004
Path: /api/chat/stream
InnerException: HttpRequestException - Response status code does not indicate success: 429 (Too Many Requests)
```

---

## Future Enhancements

### Potential Additions

1. **Retry Logic:** Automatically retry `ExternalServiceException` with exponential backoff
2. **Circuit Breaker:** Fail fast after repeated external service failures
3. **Metrics:** Track exception rates by type for monitoring
4. **Localization:** Support multiple languages for error messages
5. **Custom Headers:** Add `X-Error-Code` header for easier client-side handling
6. **Distributed Tracing:** Integrate with OpenTelemetry for cross-service correlation

---

## References

- [RFC 9110 - HTTP Semantics](https://tools.ietf.org/html/rfc9110)
- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core Error Handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Last Updated:** October 18, 2025  
**Next Review:** November 18, 2025
