# Vault Integration Deep Dive

Vault operations interface with the Model Context Protocol (MCP) to inspect and manipulate Obsidian notes. The integration ensures all file paths are normalized, auditable, and mediated through MCP tools.

## Key Conventions
- **Tool-driven actions:** File modifications (`append`, `patch`, `delete`, `create`) are executed by invoking well-known MCP tools exposed by the gateway.
- **Path normalization:** User-facing paths flow through `IVaultPathNormalizer` implementations before execution, preventing casing or separator drift.
- **Resolution via MCP listing:** `VaultPathResolver` queries the `obsidian_list_files_in_vault` tool to match user input with canonical vault paths.
- **Graceful degradation:** Both the resolver and executor detect MCP unavailability, log warnings, and return fallback results without throwing.

## Representative Code
### Executing MCP-backed file operations
```csharp
public async Task<OperationResult> AppendAsync(string filePath, string content, CancellationToken ct = default)
{
    var args = new Dictionary<string, object?>
    {
        ["filepath"] = filePath,
        ["content"] = content
    };

    return await ExecuteAsync("obsidian_append_content", args, filePath, ct);
}
```

### Fallback-aware resolver
```csharp
var client = await _mcpClientProvider.GetClientAsync(cancellationToken).ConfigureAwait(false);
if (client is null)
{
    _logger.LogDebug("MCP client unavailable; returning normalized fallback for '{Candidate}'", candidatePath);
    return fallback;
}

var toolResult = await client.CallToolAsync(
    "obsidian_list_files_in_vault",
    new Dictionary<string, object?>(),
    cancellationToken: cancellationToken).ConfigureAwait(false);

var vaultPaths = ExtractVaultPaths(toolResult.Content);
if (vaultPaths.Count == 0)
{
    _logger.LogDebug("obsidian_list_files_in_vault returned no paths; using fallback for '{Candidate}'", candidatePath);
    return fallback;
}
```

## Implementation Notes
- **Tool name contract:** MCP tool names follow the `obsidian_*` prefix and expect arguments named `filepath`, `content`, or `operation` depending on action type.
- **Result mapping:** `McpVaultToolExecutor` converts `TextContentBlock` responses into `OperationResult` messages that bubble up to the UI via use cases.
- **Regex extraction:** `RegexFileOperationExtractor` scans LLM output for Markdown file operations so that user approvals can launch MCP-backed actions.
- **Path match keys:** `IVaultPathNormalizer.CreateMatchKey` generates normalized comparison keys (lowercased, trimmed separators) used to align user input with vault inventory.
- **Cancellation discipline:** All vault calls accept `CancellationToken` inputs, mirroring upstream request lifetimes and enabling user-initiated aborts.
