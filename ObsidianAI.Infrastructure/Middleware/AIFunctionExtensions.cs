using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Extension methods for wrapping AIFunctions with middleware.
/// </summary>
public static class AIFunctionExtensions
{
    /// <summary>
    /// Wraps an AIFunction with a middleware pipeline.
    /// The middleware will execute before the function is invoked.
    /// </summary>
    /// <param name="function">The function to wrap.</param>
    /// <param name="middlewares">The middleware to apply (executes in order provided).</param>
    /// <returns>A wrapped function that executes middleware before the original function.</returns>
    public static AIFunction WithMiddleware(
        this AIFunction function,
        params IFunctionMiddleware[] middlewares)
    {
        if (function is null)
            throw new ArgumentNullException(nameof(function));

        if (middlewares is null || middlewares.Length == 0)
            return function;

        return new MiddlewareWrappedAIFunction(function, middlewares);
    }

    /// <summary>
    /// Wraps multiple AIFunctions with the same middleware pipeline.
    /// </summary>
    /// <param name="functions">The functions to wrap.</param>
    /// <param name="middlewares">The middleware to apply to all functions.</param>
    /// <returns>Collection of wrapped functions.</returns>
    public static IEnumerable<AIFunction> WithMiddleware(
        this IEnumerable<AIFunction> functions,
        params IFunctionMiddleware[] middlewares)
    {
        if (functions is null)
            throw new ArgumentNullException(nameof(functions));

        if (middlewares is null || middlewares.Length == 0)
            return functions;

        return functions.Select(f => f.WithMiddleware(middlewares));
    }
}
