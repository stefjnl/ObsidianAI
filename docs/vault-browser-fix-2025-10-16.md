# Vault Browser Folder Expansion Fix

## Issue Summary
**Date**: October 16, 2025  
**Branch**: `vault-browser`  
**Component**: Vault Browser folder expansion

When expanding a folder node in the Vault Browser UI, the application was encountering an MCP error:
```
Error 40400: Not Found
```

## Root Cause Analysis

The issue occurred due to **incomplete path construction** when listing subdirectory contents:

1. **Initial Root Listing**: When browsing the vault root (or any folder), the MCP tool `obsidian_list_files_in_vault` returns items with paths like:
   - `"AIChatAssistant/"`
   - `"AIProjectOrchestrator/"`
   - etc.

2. **Path Parsing**: The `ListVaultContentsUseCase` was correctly removing trailing slashes and storing clean paths like `"AIChatAssistant"`.

3. **Subdirectory Expansion**: When a user clicked to expand a folder, the UI passed the stored path (e.g., `"AIChatAssistant"`) to the `BrowseVaultAsync` API call.

4. **MCP Tool Call**: The API then called `obsidian_list_files_in_dir` with just `"AIChatAssistant"` as the `dirpath`.

5. **Error**: However, if the user was already browsing within a subfolder (e.g., `"🐙 Github"`), the MCP tool received an **incomplete path** and couldn't find the folder, resulting in the "404 Not Found" error.

### Example Scenario
```
User is viewing: "🐙 Github" folder
MCP returns items: ["AIChatAssistant/", "AIProjectOrchestrator/", ...]
UI stores path: "AIChatAssistant" (without parent context)
User expands folder → API calls: obsidian_list_files_in_dir("AIChatAssistant")
MCP looks for: "AIChatAssistant" (not found at vault root)
MCP should look for: "🐙 Github/AIChatAssistant"
```

## Solution Implemented

Modified `ListVaultContentsUseCase.cs` to **preserve parent path context** when parsing vault items:

### Key Changes

1. **Updated `ParseVaultItems` Signature**:
   ```csharp
   private List<VaultItemDto> ParseVaultItems(
       IEnumerable<ContentBlock>? content, 
       string? parentPath)  // NEW: added parent path parameter
   ```

2. **Added `BuildFullPath` Helper Method**:
   ```csharp
   private static string BuildFullPath(string? parentPath, string itemPath)
   {
       // If no parent path, return the item path as-is
       if (string.IsNullOrWhiteSpace(parentPath))
       {
           return itemPath;
       }

       // If itemPath already contains parent path, return as-is
       if (itemPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase))
       {
           return itemPath;
       }

       // Combine parent path with item path using forward slash
       return $"{parentPath.TrimEnd('/')}/{itemPath.TrimStart('/')}";
   }
   ```

3. **Updated Path Construction**:
   - When parsing items, the code now calls `BuildFullPath(parentPath, cleanPath)` to construct the complete path from vault root.
   - Example: `"AIChatAssistant"` becomes `"🐙 Github/AIChatAssistant"` when parent is `"🐙 Github"`.

4. **Updated `ParseAsLines` Method**:
   - Also updated to accept `parentPath` parameter and use `BuildFullPath` for consistency.

### Flow After Fix

```
User is viewing: "🐙 Github" folder
MCP returns items: ["AIChatAssistant/", "AIProjectOrchestrator/", ...]
UI stores FULL path: "🐙 Github/AIChatAssistant"
User expands folder → API calls: obsidian_list_files_in_dir("🐙 Github/AIChatAssistant")
MCP successfully finds: "🐙 Github/AIChatAssistant" ✅
MCP returns subdirectory contents
```

## Testing Recommendations

1. **Root Level Expansion**: Test expanding folders at the vault root level
2. **Nested Folder Expansion**: Test expanding folders within subfolders (2-3 levels deep)
3. **Emoji Paths**: Verify paths with emojis work correctly (e.g., `"🐙 Github"`, `"✅ Tasks"`)
4. **Error Handling**: Confirm that legitimate "not found" errors still display appropriately
5. **File Navigation**: Ensure files within nested folders can be selected and opened

## Files Modified

- `ObsidianAI.Application/UseCases/ListVaultContentsUseCase.cs`
  - Updated `ExecuteAsync` method to pass parent path to parser
  - Modified `ParseVaultItems` method signature and implementation
  - Modified `ParseAsLines` method signature and implementation
  - Added `BuildFullPath` helper method

## Related Components

- **UI Component**: `ObsidianAI.Web/Components/Shared/VaultBrowser.razor`
- **Service Layer**: `ObsidianAI.Web/Services/ChatService.cs` (`BrowseVaultAsync` method)
- **API Endpoint**: `ObsidianAI.Api/Configuration/EndpointRegistration.cs` (`/vault/browse`)
- **DTOs**: `ObsidianAI.Application/DTOs/VaultDto.cs`

## Notes

- The MCP tool `obsidian_list_files_in_dir` expects paths **without trailing slashes** (per AgentInstructions.cs)
- The MCP tool `obsidian_list_files_in_vault` returns items **with trailing slashes** for folders
- This fix maintains backward compatibility with root-level browsing while fixing nested folder expansion
