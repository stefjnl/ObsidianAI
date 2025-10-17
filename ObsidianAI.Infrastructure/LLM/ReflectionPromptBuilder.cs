using System.Collections.Generic;
using System.Text.Json;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Builds prompts for the reflection LLM to analyze file operations for safety.
/// </summary>
public class ReflectionPromptBuilder
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a reflection prompt for the given tool operation.
    /// </summary>
    /// <param name="toolName">The name of the tool being invoked.</param>
    /// <param name="arguments">The arguments passed to the tool.</param>
    /// <returns>A formatted prompt for the reflection LLM.</returns>
    public string BuildPrompt(string toolName, IReadOnlyDictionary<string, object?> arguments)
    {
        var argumentsJson = JsonSerializer.Serialize(arguments, _jsonOptions);

        var prompt = $@"You are validating a file operation for safety in an Obsidian vault management system.

Operation: {toolName}
Arguments: {argumentsJson}

Validation Criteria:
1. File path is exact and unambiguous (no wildcards, clear target)
2. Operation is reversible OR user has explicitly confirmed through the UI
3. Minimal data loss risk (no bulk deletes, no overwriting without backup)
4. Path safety (no system directories, no dangerous paths)

IMPORTANT: The presence of a 'confirm' parameter in the arguments does NOT mean the user has confirmed. 
That parameter is from the MCP tool schema, not user input. Always require confirmation for destructive operations.

Operation-specific validation:
- obsidian_delete_file: ALWAYS needs confirmation (set needsUserConfirmation=true)
- obsidian_patch_content: ALWAYS needs confirmation (set needsUserConfirmation=true)
- obsidian_move_file: ALWAYS needs confirmation (set needsUserConfirmation=true)
- obsidian_append_content: Generally safe, low risk
- obsidian_list_directory: Safe, read-only
- obsidian_search: Safe, read-only

Respond with JSON in this exact format:
{{
  ""shouldReject"": true/false,
  ""needsUserConfirmation"": true/false,
  ""reason"": ""brief explanation of decision"",
  ""actionDescription"": ""human-readable description of what will happen"",
  ""safetyChecks"": [""check1"", ""check2""],
  ""warnings"": [""warning1""]
}}

Guidelines:
- Be conservative: when in doubt, request confirmation
- Reject operations that are clearly dangerous or malformed
- For delete, patch, and move operations: ALWAYS set needsUserConfirmation=true
- Ignore any 'confirm' parameter in arguments - it's a tool schema field, not user confirmation
- Keep reason and actionDescription concise but informative
- List specific safety checks performed
- Include warnings for potential issues that don't block the operation
";

        return prompt;
    }
}