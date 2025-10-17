using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Defines middleware that can intercept and modify function invocations.
/// This middleware runs between the agent's decision to call a function and the actual function execution.
/// </summary>
public interface IFunctionMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The function invocation context containing function metadata and arguments.</param>
    /// <param name="next">Delegate to invoke the next middleware or the actual function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function invocation, or a custom result if middleware terminates early.</returns>
    ValueTask<object?> InvokeAsync(
        FunctionInvocationContext context,
        Func<ValueTask<object?>> next,
        CancellationToken cancellationToken);
}
