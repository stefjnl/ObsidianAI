# Function Middleware Wrapper System - Implementation Complete ✅

## Overview
Successfully implemented a middleware wrapper system that intercepts MCP tool calls at the `AIFunction` level, allowing inspection, logging, and blocking of operations before execution.

## Implementation Approach

### Why This Approach?
The Microsoft Agent Framework documentation showed middleware with an `AIAgent` parameter, but this doesn't exist in the actual `Microsoft.Extensions.AI` API. Instead, we implemented middleware by **wrapping `AIFunction` objects** before passing them to the agent.

### Architecture
```
MCP Tools → Wrapped with Middleware → Passed to Agent → Agent calls tool → Middleware executes
```

## Files Created

### 1. `IFunctionMiddleware.cs`
**Purpose**: Defines the middleware contract.

```csharp
public interface IFunctionMiddleware
{
    ValueTask<object?> InvokeAsync(
        FunctionInvocationContext context,
        Func<ValueTask<object?>> next,
        CancellationToken cancellationToken);
}
```

**Key Points**:
- No `AIAgent` parameter (doesn't exist in API)
- Simple `next` delegate (no parameters needed)
- Returns `ValueTask<object?>` for efficiency

### 2. `FunctionInvocationContext.cs`
**Purpose**: Context object that middleware can inspect and modify.

**Properties**:
- `AIFunction Function` - The function being invoked
- `IReadOnlyDictionary<string, object?> Arguments` - Function arguments  
- `bool Terminate` - Set to true to abort execution
- `object? Result` - Custom result when terminated

**Why a custom context?**  
- Provides clean separation from Microsoft.Extensions.AI types
- Allows middleware to control execution flow via `Terminate`
- Encapsulates all data middleware needs to make decisions

### 3. `MiddlewareWrappedAIFunction.cs`
**Purpose**: Wraps an `AIFunction` to execute middleware pipeline before invocation.

**Key Features**:
- Inherits from `AIFunction` (works seamlessly with agent)
- Builds middleware chain in reverse order (last wraps first)
- Respects `context.Terminate` to short-circuit execution
- Uses `ValueTask<object?>` return type (per API requirements)
- Passes through function name via `Name` property (not `Metadata`)

**Pipeline Execution**:
```
Middleware 1 → Middleware 2 → ... → Actual Function
```

### 4. `AIFunctionExtensions.cs`
**Purpose**: Extension methods for easy middleware attachment.

```csharp
var wrappedTools = tools
    .OfType<AIFunction>()
    .WithMiddleware(middleware1, middleware2)
    .Cast<object>();
```

### 5. Updated `TestFunctionCallMiddleware.cs`
**Changes**:
- Removed `AIAgent` parameter
- Implements `IFunctionMiddleware` interface
- Uses `context.Function.Name` (not `Metadata.Name` per official docs)
- Sets `context.Terminate` and `context.Result` when blocking

### 6. Updated `ConfiguredAIAgentFactory.cs`
**Changes**:
- Accepts `IEnumerable<IFunctionMiddleware>` instead of concrete type
- Wraps tools with middleware before passing to agent:

```csharp
var wrappedTools = tools is not null && _middlewares.Count > 0
    ? tools.OfType<AIFunction>()
           .WithMiddleware(_middlewares.ToArray())
           .Cast<object>()
    : tools;
```

### 7. Updated `LmStudioChatAgent.cs` & `OpenRouterChatAgent.cs`
**Changes**:
- Removed middleware parameters from constructors
- Removed `AttachFunctionMiddlewares` helper methods
- Simplified to just accept tools (already wrapped by factory)

### 8. Updated `ServiceCollectionExtensions.cs`
**DI Registration**:
```csharp
// Register middleware
services.AddSingleton<IFunctionMiddleware, TestFunctionCallMiddleware>();

// Register factory with middleware injection
services.AddSingleton<IAIAgentFactory>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AppSettings>>();
    var middlewares = sp.GetServices<IFunctionMiddleware>();
    return new ConfiguredAIAgentFactory(options, middlewares);
});
```

### 9. Updated Test Suite
**Changes**:
- Added type alias: `using FunctionContext = ObsidianAI.Infrastructure.Middleware.FunctionInvocationContext;`
- Updated all test methods to use new signature (no `AIAgent`, simpler `Next` delegate)
- Fixed `CreateContext` helper to return custom context type

## Build & Test Status

```
✅ Solution builds successfully
✅ All 3 middleware tests pass
✅ No compilation errors
✅ No warnings
```

**Test Coverage**:
1. ✅ Allows non-destructive operations to proceed
2. ✅ Blocks delete operations with custom message
3. ✅ Recovers gracefully from middleware errors

## How It Works

### 1. Registration (DI)
```csharp
services.AddSingleton<IFunctionMiddleware, TestFunctionCallMiddleware>();
```

### 2. Factory Receives Middleware
```csharp
public ConfiguredAIAgentFactory(
    IOptions<AppSettings> options,
    IEnumerable<IFunctionMiddleware> middlewares)
```

### 3. Tools Are Wrapped
```csharp
var wrappedTools = tools
    .OfType<AIFunction>()
    .WithMiddleware(_middlewares.ToArray());
```

### 4. Agent Calls Tool
When agent decides to call a tool, it invokes the wrapped function.

### 5. Middleware Pipeline Executes
```
TestFunctionCallMiddleware.InvokeAsync()
  → Logs function name and arguments
  → Checks if "obsidian_delete_file"
  → If yes: sets context.Terminate = true, returns custom message
  → If no: calls next() → executes actual function
```

### 6. Result Returns to Agent
Either the custom blocked message or the actual function result.

## Verification Steps

### ✅ Completed
1. Build solution - **SUCCESS**
2. Run unit tests - **3/3 PASS**

### 🔄 Remaining Runtime Verification
1. Start Aspire host: `dotnet run --project ObsidianAI.AppHost`
2. Send chat message that triggers MCP tool calls
3. Check logs for middleware interception messages
4. Attempt delete operation - should be blocked
5. Perform safe operations (append, list) - should proceed

## Expected Runtime Behavior

### Delete Operation
```
[Information] Intercepting obsidian_delete_file at 2025-01-17T... with arguments {"path":"vault/note.md"}
[Warning] ⚠️ DESTRUCTIVE OPERATION DETECTED for obsidian_delete_file
Result: "DELETE BLOCKED BY TEST MIDDLEWARE"
```

### Safe Operation
```
[Information] Intercepting obsidian_append_content at 2025-01-17T... with arguments {"path":"vault/note.md","content":"..."}
[Information] Function obsidian_append_content completed with result {...}
Result: (actual MCP response)
```

### Error Recovery
```
[Error] Test middleware failed for obsidian_list_files; delegating to next middleware
[Information] Function obsidian_list_files completed with result {...} after middleware error
Result: (operation proceeds despite middleware error)
```

## Key Design Decisions

### Why Wrap Functions Instead of Using ChatClient Middleware?
- `UseFunctionInvocation` on `ChatClientBuilder` doesn't accept custom delegates
- It creates its own `FunctionInvokingChatClient` wrapper
- Wrapping `AIFunction` directly gives us full control over execution

### Why Custom FunctionInvocationContext?
- Clean separation from Microsoft.Extensions.AI internals
- Provides `Terminate` flag for blocking operations
- Allows setting custom `Result` messages
- Avoids namespace conflicts (we had `Microsoft.Extensions.AI.FunctionInvocationContext` collision)

### Why ValueTask Instead of Task?
- Per official API: `AIFunction.InvokeCoreAsync` returns `ValueTask<object?>`
- More efficient for synchronous or frequently-called operations
- Required to override base class method correctly

### Why No AIAgent Parameter?
- Doesn't exist in `Microsoft.Extensions.AI` API
- Official docs showed it but actual implementation doesn't have it
- Our middleware doesn't need agent reference anyway

## Integration Points

### MCP Tools Flow
```
MCP Docker Gateway 
  → ListToolsAsync returns AIFunction[]
  → ConfiguredAIAgentFactory wraps with middleware
  → Passed to ChatClientAgent constructor
  → Agent invokes tool
  → Middleware executes
  → MCP tool called (if not terminated)
```

### Multiple Middlewares
Can register multiple middlewares - they execute in order:
```csharp
services.AddSingleton<IFunctionMiddleware, TestFunctionCallMiddleware>();
services.AddSingleton<IFunctionMiddleware, AuditMiddleware>();
services.AddSingleton<IFunctionMiddleware, ValidationMiddleware>();

// Execution order: Test → Audit → Validation → Function
```

## Next Phase: Production Reflection Middleware

This test middleware validates the infrastructure. Next steps:

1. Create `ReflectionFunctionMiddleware` implementing `IFunctionMiddleware`
2. On destructive operations:
   - Prompt LLM to explain operation impact
   - Generate action card for user approval
   - Store decision in audit trail
3. Register alongside or replace `TestFunctionCallMiddleware`
4. No changes to agent factory or DI - just swap middleware

## Troubleshooting

### If Middleware Doesn't Execute
- Check DI registration includes middleware
- Verify factory wraps tools: `tools.WithMiddleware(_middlewares.ToArray())`
- Ensure tools are cast to `AIFunction` before wrapping
- Confirm agent receives wrapped tools, not original

### If Tests Fail
- Verify using `using FunctionContext = ObsidianAI.Infrastructure.Middleware.FunctionInvocationContext;`
- Check `Next` delegate has no parameters: `ValueTask<object?> Next()`
- Ensure context creation uses our custom `FunctionContext`

### If Build Fails with Metadata Error
- Use `context.Function.Name` not `context.Function.Metadata.Name`
- `AIFunction` has `Name` property directly (per official docs)

## Summary

✅ **Complete middleware wrapper system implemented**
✅ **Wraps AIFunction objects before passing to agent**  
✅ **Middleware executes between agent decision and function execution**
✅ **Supports termination, logging, and error recovery**
✅ **Chain of responsibility pattern for multiple middlewares**
✅ **All tests passing**
✅ **Ready for runtime verification**

**Status**: Phase 1 Complete - Middleware Infrastructure Validated
**Next**: Runtime testing with actual MCP tools, then production reflection middleware
