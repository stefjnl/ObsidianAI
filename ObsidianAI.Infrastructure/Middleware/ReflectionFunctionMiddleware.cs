using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Services;
using ObsidianAI.Infrastructure.Services;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Middleware that uses reflection LLM to validate destructive operations before execution.
/// </summary>
public class ReflectionFunctionMiddleware : IFunctionMiddleware
{
    private readonly IReflectionService _reflectionService;
    private readonly IAgentStateService _stateService;
    private readonly ILogger<ReflectionFunctionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the ReflectionFunctionMiddleware.
    /// </summary>
    public ReflectionFunctionMiddleware(
        IReflectionService reflectionService,
        IAgentStateService stateService,
        ILogger<ReflectionFunctionMiddleware> logger)
    {
        _reflectionService = reflectionService ?? throw new ArgumentNullException(nameof(reflectionService));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        FunctionInvocationContext context,
        Func<ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var functionName = context.Function?.Name ?? "<unknown>";
        var arguments = context.Arguments ?? new Dictionary<string, object?>();

        Console.WriteLine($"[REFLECTION-MIDDLEWARE] 🔍 INVOKED for function: {functionName}");
        Console.WriteLine($"[REFLECTION-MIDDLEWARE] 📋 Arguments: {System.Text.Json.JsonSerializer.Serialize(arguments)}");
        _logger.LogInformation("🔍 Reflection middleware invoked for function: {FunctionName}", functionName);

        try
        {
            // Check if operation is destructive
            if (!IsDestructive(functionName))
            {
                Console.WriteLine($"[REFLECTION-MIDDLEWARE] ⏭️ Skipping non-destructive: {functionName}");
                _logger.LogDebug("Skipping reflection for non-destructive operation: {FunctionName}", functionName);
                return await next().ConfigureAwait(false);
            }

            Console.WriteLine($"[REFLECTION-MIDDLEWARE] ⚠️ DESTRUCTIVE OPERATION DETECTED: {functionName}");
            _logger.LogInformation("Starting reflection analysis for destructive operation: {FunctionName}", functionName);

            // Call reflection service
            var reflection = await _reflectionService.ReflectAsync(functionName, arguments, cancellationToken).ConfigureAwait(false);

            // If rejected: block immediately
            if (reflection.ShouldReject)
            {
                _logger.LogWarning("Reflection rejected operation {FunctionName}: {Reason}", functionName, reflection.Reason);
                context.Terminate = true;
                context.Result = $"REJECTED: {reflection.Reason}";
                return context.Result;
            }

            // If needs confirmation: store metadata, build ActionCard, and block
            if (reflection.NeedsUserConfirmation)
            {
                var metadataKey = $"reflection_{Guid.NewGuid()}";
                
                // Store both reflection result AND original operation context for later execution
                var operationContext = new
                {
                    FunctionName = functionName,
                    Arguments = arguments,
                    Reflection = reflection
                };
                _stateService.Set(metadataKey, operationContext);

                _logger.LogInformation("Reflection requires confirmation for {FunctionName}, stored with key {Key}", functionName, metadataKey);

                // Build ActionCard JSON for the UI (now includes reflection key)
                var actionCardJson = ActionCardBuilder.BuildActionCardJson(reflection, functionName, arguments, metadataKey);

                context.Terminate = true;
                context.Result = new
                {
                    Status = "PENDING_CONFIRMATION",
                    ReflectionKey = metadataKey,
                    Description = reflection.ActionDescription ?? "Operation requires confirmation",
                    Warnings = reflection.Warnings,
                    ActionCardJson = actionCardJson
                };
                return context.Result;
            }

            // If approved: proceed with operation
            _logger.LogInformation("Reflection approved operation {FunctionName}: {Reason}", functionName, reflection.Reason);
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Never block operations due to reflection failures
            _logger.LogError(ex, "Reflection middleware failed for {FunctionName}, allowing operation to proceed", functionName);
            return await next().ConfigureAwait(false);
        }
    }

    private static bool IsDestructive(string toolName) =>
        toolName is "obsidian_delete_file" or "obsidian_patch_content" or "obsidian_move_file";
}