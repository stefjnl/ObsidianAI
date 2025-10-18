## Context
You are working on ObsidianAI, a .NET 9 application that provides an AI-powered chat interface for Obsidian vaults. The application follows Clean Architecture with separate Domain, Application, Infrastructure, API, and Web layers.

## Current Problem
When users click on a file in the Vault Browser to view its contents, the LLM processes the file content and adds conversational wrapper text like "The content of the file has been retrieved and is provided below:". We need raw, unprocessed file content for display purposes, while still supporting conversational queries about files through the chat interface.

## Implementation Goal
Implement **Option 1 + Option 4 Combined**: Separate endpoints and services for vault operations vs. chat operations.

## Required Changes

### 1. API Layer (ObsidianAI.Api)

**Create new endpoint group `/vault`:**
- `GET /vault/read?path={filePath}` - Returns raw file content (bypass LLM)
- `GET /vault/structure` - Returns vault folder/file tree structure
- Keep existing `/chat` endpoint for LLM-powered interactions

**Implementation notes:**
- These endpoints should call MCP directly without involving the LLM
- Return raw markdown content as plain text or JSON wrapper
- Use existing `McpClientService` for MCP communication
- Add proper error handling for file not found scenarios

### 2. Application Layer (ObsidianAI.Application)

**Create new use case:**
- `ReadFileUseCase` - Directly calls MCP to read file content, returns raw result
  - Input: file path
  - Output: raw file content string
  - No LLM involvement
  
**Keep existing:**
- `StreamChatUseCase` - For conversational AI interactions (already exists)

**Implementation notes:**
- `ReadFileUseCase` should use `IMcpClientProvider` to get MCP client
- Call appropriate MCP tool (`obsidian_get_file_contents`) for consistency with the new use case
- Extract text from `TextContentBlock` in the response
- No streaming needed for file reads

### 3. Infrastructure Layer (ObsidianAI.Infrastructure)

**If needed, create:**
- Any additional MCP tool mappings for vault browsing operations
- Ensure `McpVaultToolExecutor` or equivalent can handle read-only operations

**Implementation notes:**
- May not need changes if MCP client already exposes necessary tools
- Verify which MCP tools are available for file reading and directory listing

### 4. Web Layer (ObsidianAI.Web)

**Create new service:**
- `IVaultService` interface with methods:
  - `Task<string> ReadFileAsync(string path)`
  - `Task<VaultStructure> GetVaultStructureAsync()`
- `VaultService` implementation that calls API endpoints

**Keep existing:**
- `ChatService` - For conversational interactions (already exists)

**Update components:**
- `VaultBrowser.razor` (if it exists) or create it:
  - Use `VaultService` for file operations
  - Display raw markdown in viewer pane
  - No ActionCards or AI processing
  
- `Chat.razor` (already exists):
  - Continue using `ChatService`
  - Keep existing ActionCard logic
  - Conversational AI interactions

**Implementation notes:**
- `VaultService` should use HttpClient to call `/vault/*` endpoints
- Keep clean separation: VaultBrowser never calls ChatService, Chat never calls VaultService
- Consider adding a markdown viewer component for displaying raw .md files

### 5. Domain Layer (ObsidianAI.Domain)

**Potentially add:**
- `VaultStructure` model for representing vault tree structure
- Keep existing models unchanged

## Expected Behavior After Implementation

### Scenario 1: User clicks file in Vault Browser
```
1. User clicks "Code Review Checklist.md" in VaultBrowser
2. VaultBrowser calls VaultService.ReadFileAsync(path)
3. VaultService calls GET /vault/read?path=...
4. API endpoint calls ReadFileUseCase
5. ReadFileUseCase calls MCP directly (no LLM)
6. Raw markdown returned through stack
7. VaultBrowser displays raw content in viewer
```

### Scenario 2: User asks about file in Chat
```
1. User types "Summarize the code review checklist"
2. Chat calls ChatService.SendMessage(...)
3. ChatService calls POST /chat
4. API uses StreamChatUseCase with LLM
5. LLM uses MCP tools to read file and generates summary
6. Summary streamed back to Chat
7. User sees conversational response
```

## Technical Constraints

- Use existing `McpClientService` for MCP communication
- Follow existing patterns in the codebase for dependency injection
- Maintain Clean Architecture layer boundaries
- Use existing error handling patterns
- Keep existing `StreamChatUseCase` unchanged
- Ensure proper async/await throughout

## Testing Requirements

After implementation, verify:
1. File reads return raw content without LLM commentary
2. Chat queries about files still work conversationally
3. No performance regression (file reads should be faster without LLM)
4. Error handling works for missing files
5. Both services can coexist without conflicts

## Non-Goals (Don't Implement)

- Don't modify existing chat functionality
- Don't change existing MCP tool execution in StreamChatUseCase
- Don't add new UI components beyond what's needed for vault browsing
- Don't implement file modification through VaultService (use Chat/ActionCards)

## Questions to Resolve During Implementation

1. What is the exact MCP tool name for reading file contents?
2. Does a VaultBrowser component already exist, or should it be created?
3. Should vault structure be cached or fetched on every request?
4. What format should GET /vault/read return? Plain text or JSON wrapped?

## Priority

Implement in this order:
1. ReadFileUseCase in Application layer
2. GET /vault/read endpoint in API layer
3. VaultService in Web layer
4. Update or create VaultBrowser component
5. Add tests

---

**Implementation Style Notes:**
- Follow existing code conventions in the project
- Use dependency injection patterns already established
- Keep similar error handling patterns
- Match existing API response formats
- Use existing logging patterns

Please implement these changes following Clean Architecture principles and the existing patterns in the ObsidianAI codebase.