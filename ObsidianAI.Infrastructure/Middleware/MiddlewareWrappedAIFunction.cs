using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Wraps an AIFunction to execute middleware before invoking the actual function.
/// This enables interception, logging, validation, and blocking of function calls.
/// </summary>
/// <remarks>
/// The middleware pipeline executes in the order provided, with each middleware
/// having the opportunity to inspect, modify, or terminate the invocation.
/// If any middleware sets context.Terminate = true, the pipeline short-circuits
/// and returns the custom result without executing subsequent middleware or the function.
/// </remarks>
public sealed class MiddlewareWrappedAIFunction : AIFunction
{
    private readonly AIFunction _innerFunction;
    private readonly IReadOnlyList<IFunctionMiddleware> _middlewares;

    /// <summary>
    /// Initializes a new instance of the <see cref="MiddlewareWrappedAIFunction"/> class.
    /// </summary>
    /// <param name="innerFunction">The actual function to wrap.</param>
    /// <param name="middlewares">The middleware pipeline to execute before the function.</param>
    public MiddlewareWrappedAIFunction(
        AIFunction innerFunction,
        IEnumerable<IFunctionMiddleware> middlewares)
    {
        _innerFunction = innerFunction ?? throw new ArgumentNullException(nameof(innerFunction));
        _middlewares = middlewares?.ToList() ?? throw new ArgumentNullException(nameof(middlewares));

        // LOG: Wrapper created
        Console.WriteLine($"[WRAPPER] ✅ Created for: {_innerFunction.Name} with {_middlewares.Count} middlewares");
    }

    /// <summary>
    /// Gets the name of the wrapped function.
    /// </summary>
    public override string Name => _innerFunction.Name;

    /// <summary>
    /// Gets the description of the wrapped function.
    /// </summary>
    public override string Description => _innerFunction.Description ?? string.Empty;

    /// <summary>
    /// Gets the JSON schema of the wrapped function.
    /// </summary>
    public override JsonElement JsonSchema => _innerFunction.JsonSchema;

    /// <summary>
    /// Gets the return JSON schema of the wrapped function.
    /// </summary>
    public override JsonElement? ReturnJsonSchema => _innerFunction.ReturnJsonSchema;

    /// <summary>
    /// Gets the additional properties of the wrapped function.
    /// </summary>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _innerFunction.AdditionalProperties;

    /// <summary>
    /// Gets the JSON serializer options of the wrapped function.
    /// </summary>
    public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions ?? new JsonSerializerOptions();

    /// <summary>
    /// Invokes the function with middleware pipeline.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result from the function or middleware.</returns>
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // Convert AIFunctionArguments to dictionary for middleware inspection
        var argsDict = new Dictionary<string, object?>();
        if (arguments is not null)
        {
            foreach (var arg in arguments)
            {
                argsDict[arg.Key] = arg.Value;
            }
        }

        // Create context that middleware can inspect and modify
        var context = new FunctionInvocationContext(
            function: _innerFunction,
            arguments: argsDict
        );

        // Build middleware pipeline starting with the actual function execution
        Func<ValueTask<object?>> pipeline = () => ExecuteInnerFunctionAsync(arguments ?? new AIFunctionArguments(), cancellationToken);

        // Chain middlewares in reverse order (last middleware wraps the function)
        // This creates a nested chain: middleware1 -> middleware2 -> ... -> function
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = pipeline;

            // Capture the current middleware and next delegate in closure
            pipeline = () => middleware.InvokeAsync(context, next, cancellationToken);
        }

        // Execute the pipeline (starts with first middleware)
        var result = await pipeline().ConfigureAwait(false);

        // If any middleware set Terminate=true, return the custom result instead
        if (context.Terminate)
        {
            return context.Result;
        }

        return result;
    }

    /// <summary>
    /// Executes the inner function with the provided arguments.
    /// This is the final step in the middleware pipeline.
    /// </summary>
    private async ValueTask<object?> ExecuteInnerFunctionAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        return await _innerFunction.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
