using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Test middleware that intercepts function calls to validate middleware infrastructure.
/// Blocks destructive operations (delete) for testing termination logic.
/// </summary>
/// <remarks>
/// This is temporary test code. Once we verify interception works,
/// we'll replace it with production reflection middleware.
/// </remarks>
public sealed class TestFunctionCallMiddleware : IFunctionMiddleware
{
    private readonly ILogger<TestFunctionCallMiddleware> _logger;

    public TestFunctionCallMiddleware(ILogger<TestFunctionCallMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOG: Middleware constructed
        _logger.LogWarning("⚡ TestFunctionCallMiddleware CONSTRUCTED");
        Console.WriteLine("========================================");
        Console.WriteLine("⚡ TestFunctionCallMiddleware CONSTRUCTED");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// Middleware invocation method that intercepts function calls.
    /// </summary>
    /// <param name="context">The function invocation context.</param>
    /// <param name="next">Delegate to invoke the next middleware or actual function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The function result, or a custom message if blocked.</returns>
    public async ValueTask<object?> InvokeAsync(
        FunctionInvocationContext context,
        Func<ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // Null safety: context should never be null, but defensive programming
        if (context is null)
        {
            _logger.LogError("Function invocation context was null");
            return null;
        }

        var functionName = context.Function?.Name ?? "<unknown>";
        var timestamp = DateTimeOffset.UtcNow;

        string argumentsJson;
        try
        {
            argumentsJson = context.Arguments is null
                ? "null"
                : JsonSerializer.Serialize(context.Arguments);
        }
        catch (Exception serializationError)
        {
            _logger.LogError(serializationError, "Failed to serialize arguments for {FunctionName}", functionName);
            argumentsJson = "<unserializable>";
        }

        try
        {
            _logger.LogInformation(
                "Intercepting {FunctionName} at {InvocationTimestamp:u} with arguments {ArgumentsJson}",
                functionName,
                timestamp,
                argumentsJson);

            if (string.Equals(functionName, "obsidian_delete_file", StringComparison.Ordinal))
            {
                // Why block delete operations specifically?
                // Answer: They're irreversible and high-risk, perfect for testing termination logic.
                _logger.LogWarning("⚠️ DESTRUCTIVE OPERATION DETECTED for {FunctionName}", functionName);

                // Terminate the function call by setting Terminate flag and providing custom result
                context.Terminate = true;
                context.Result = "DELETE BLOCKED BY TEST MIDDLEWARE";

                // Return custom error message instead of executing function
                return context.Result;
            }

            // Call the next middleware or the actual function
            var result = await next().ConfigureAwait(false);

            _logger.LogInformation("Function {FunctionName} completed with result {@Result}", functionName, result);

            return result;
        }
        catch (Exception ex)
        {
            // Why always call next() on errors?
            // Answer: Middleware failures shouldn't break user workflows.
            _logger.LogError(ex, "Test middleware failed for {FunctionName}; delegating to next middleware", functionName);

            // Always delegate to next on error - don't block operations due to middleware bugs
            var result = await next().ConfigureAwait(false);

            _logger.LogInformation(
                "Function {FunctionName} completed with result {@Result} after middleware error",
                functionName,
                result);

            return result;
        }
    }
}
