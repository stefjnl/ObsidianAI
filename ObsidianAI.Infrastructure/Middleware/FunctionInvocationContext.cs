using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Context for function invocation that middleware can inspect and modify.
/// Middleware can read function metadata, inspect arguments, terminate execution, or provide custom results.
/// </summary>
public sealed class FunctionInvocationContext
{
    /// <summary>
    /// The function being invoked.
    /// </summary>
    public AIFunction Function { get; }

    /// <summary>
    /// The arguments being passed to the function.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>
    /// When set to true, terminates the invocation without calling the actual function.
    /// The middleware should set <see cref="Result"/> to provide a return value.
    /// </summary>
    public bool Terminate { get; set; }

    /// <summary>
    /// Custom result to return if <see cref="Terminate"/> is true.
    /// This allows middleware to block operations and provide explanatory messages.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvocationContext"/> class.
    /// </summary>
    /// <param name="function">The function being invoked.</param>
    /// <param name="arguments">The arguments being passed to the function.</param>
    public FunctionInvocationContext(
        AIFunction function,
        IReadOnlyDictionary<string, object?> arguments)
    {
        Function = function ?? throw new ArgumentNullException(nameof(function));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }
}
