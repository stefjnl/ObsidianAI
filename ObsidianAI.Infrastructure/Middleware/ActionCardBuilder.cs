using System;
using System.Collections.Generic;
using System.Text.Json;
using ObsidianAI.Domain.Models;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Builds ActionCard JSON payloads from reflection results for server-side ActionCard creation.
/// </summary>
public static class ActionCardBuilder
{
    /// <summary>
    /// Builds an ActionCard JSON payload from a reflection result and function call context.
    /// </summary>
    /// <param name="reflection">The reflection result from the LLM.</param>
    /// <param name="functionName">The name of the function being called.</param>
    /// <param name="arguments">The arguments passed to the function.</param>
    /// <param name="reflectionKey">The key used to retrieve the stored operation for confirmation.</param>
    /// <returns>A JSON string representing the ActionCard data.</returns>
    public static string BuildActionCardJson(ReflectionResult reflection, string functionName, IReadOnlyDictionary<string, object?> arguments, string reflectionKey)
    {
        ArgumentNullException.ThrowIfNull(reflection);
        ArgumentNullException.ThrowIfNull(functionName);
        ArgumentNullException.ThrowIfNull(arguments);

        // Extract file path from arguments
        var filePath = ExtractFilePath(arguments);
        var operation = MapFunctionToOperation(functionName);
        var actionType = MapFunctionToActionType(functionName);

        // Build planned action
        var plannedAction = new
        {
            id = Guid.NewGuid().ToString(),
            type = actionType,
            source = filePath ?? string.Empty,
            destination = ExtractDestination(functionName, arguments) ?? filePath ?? string.Empty,
            description = reflection.ActionDescription ?? $"{operation} {filePath}",
            operation = operation.ToLowerInvariant(),
            content = ExtractContent(arguments),
            sortOrder = 0
        };

        // Build ActionCard payload
        var actionCard = new
        {
            id = Guid.NewGuid().ToString(),
            title = $"{char.ToUpper(operation[0])}{operation.Substring(1)} Operation",
            status = "Pending",
            operation = operation.ToLowerInvariant(),
            statusMessage = string.Empty,
            createdAt = DateTime.UtcNow,
            completedAt = (DateTime?)null,
            plannedActions = new[] { plannedAction },
            reflectionMetadata = new
            {
                reasoning = reflection.Reason,
                warnings = reflection.Warnings,
                needsConfirmation = reflection.NeedsUserConfirmation,
                reflectionKey = reflectionKey
            }
        };

        return JsonSerializer.Serialize(actionCard);
    }

    private static string? ExtractFilePath(IReadOnlyDictionary<string, object?> arguments)
    {
        // Try common parameter names
        if (arguments.TryGetValue("filepath", out var filepath) && filepath != null)
            return filepath.ToString();
        
        if (arguments.TryGetValue("path", out var path) && path != null)
            return path.ToString();
        
        if (arguments.TryGetValue("source", out var source) && source != null)
            return source.ToString();

        return null;
    }

    private static string? ExtractDestination(string functionName, IReadOnlyDictionary<string, object?> arguments)
    {
        // Only relevant for move operations
        if (functionName == "obsidian_move_file" && arguments.TryGetValue("destination", out var dest) && dest != null)
            return dest.ToString();

        return null;
    }

    private static string? ExtractContent(IReadOnlyDictionary<string, object?> arguments)
    {
        if (arguments.TryGetValue("content", out var content) && content != null)
            return content.ToString();

        return null;
    }

    private static string MapFunctionToOperation(string functionName) => functionName switch
    {
        "obsidian_delete_file" => "Delete",
        "obsidian_patch_content" => "Patch",
        "obsidian_move_file" => "Move",
        _ => "Modify"
    };

    private static string MapFunctionToActionType(string functionName) => functionName switch
    {
        "obsidian_delete_file" => "Delete",
        "obsidian_patch_content" => "Modify",
        "obsidian_move_file" => "Move",
        _ => "Other"
    };
}
