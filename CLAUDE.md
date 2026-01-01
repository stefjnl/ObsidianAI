# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ObsidianAI: .NET Aspire app providing AI-powered natural language interaction with Obsidian vaults via LLMs (OpenRouter/LMStudio/NanoGPT) and Model Context Protocol (MCP).

**Stack**: .NET 9, Blazor Server, SignalR, EF Core + SQLite, Microsoft Agents Framework, MCP

## Commands

### Build & Run
```bash
# Run via Aspire (starts Web + MCP Gateway)
dotnet run --project ObsidianAI.AppHost

# Run via Docker Compose
docker compose up --build

# Build solution
dotnet build

# Restore dependencies
dotnet restore
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test ObsidianAI.Tests

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Database
```bash
# Create migration (from Infrastructure project)
dotnet ef migrations add MigrationName --project ObsidianAI.Infrastructure --startup-project ObsidianAI.Web

# Apply migrations (automatic on app startup, or manually)
dotnet ef database update --project ObsidianAI.Infrastructure --startup-project ObsidianAI.Web

# Remove last migration
dotnet ef migrations remove --project ObsidianAI.Infrastructure --startup-project ObsidianAI.Web
```

### Configuration
```bash
# Set user secrets (OpenRouter API key)
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-v1-..." --project ObsidianAI.Web

# Set LMStudio API key
dotnet user-secrets set "LLM:LMStudio:ApiKey" "your-key" --project ObsidianAI.Web

# List user secrets
dotnet user-secrets list --project ObsidianAI.Web
```

## Architecture

**Clean Architecture** with strict dependency flow: `Web → Application → Domain ← Infrastructure`

### Layer Responsibilities

**Domain** (`ObsidianAI.Domain`)
- Core entities: `Conversation`, `Message`, `ActionCardRecord`, `PlannedActionRecord`, `FileOperationRecord`, `Attachment`
- Value objects: `ChatInput`, `ChatStreamEvent`, `FileOperation`, `ReflectionResult`
- Ports (interfaces): `IChatAgent`, `IConversationRepository`, `IMessageRepository`, `IVaultToolExecutor`, `IReflectionService`
- No external dependencies

**Application** (`ObsidianAI.Application`)
- Use Cases: `StreamChatUseCase`, `StartChatUseCase`, `ModifyVaultUseCase`, `ListVaultContentsUseCase`, `ReadFileUseCase`, `SearchVaultUseCase`, `CreateConversationUseCase`, `LoadConversationUseCase`, etc.
- Services: `IMcpToolCatalog`, `IVaultPathResolver`, `IFileOperationExtractor`, `IVaultPathNormalizer`
- DTOs: `ConversationDto`, `MessageDto`, `ConversationDetailDto`
- Depends only on Domain

**Infrastructure** (`ObsidianAI.Infrastructure`)
- Repositories: `ConversationRepository`, `MessageRepository`, `AttachmentRepository` (EF Core)
- LLM Clients: `LmStudioChatAgent`, `OpenRouterChatAgent`, `NanoGptChatAgent` (all inherit `BaseChatAgent`)
- Agent Factory: `ConfiguredAIAgentFactory` (IAIAgentFactory)
- Middleware: `ReflectionFunctionMiddleware` (safety validation), `GlobalExceptionHandlingMiddleware`
- MCP Integration: `McpVaultToolExecutor`, `InMemoryAgentThreadProvider`
- DbContext: `ObsidianAIDbContext` (SQLite)
- Configuration: Strongly-typed `AppSettings` POCOs
- Depends on Domain and Application interfaces

**Web** (`ObsidianAI.Web`)
- Blazor Server components: `Chat.razor`, `ConversationSidebar.razor`, `ChatArea.razor`, `VaultBrowser.razor`, `ActionCard.razor`
- SignalR Hub: `ChatHub` (real-time streaming with token batching)
- REST API Endpoints: `/api/conversations`, `/api/actioncards`, `/api/vault`, `/api/providers`
- Services: `McpClientService` (hosted service), `MicrosoftLearnMcpClient`, `VaultResizeService`
- HTTP Services: `ChatService`, `VaultService` (for Blazor components)
- Depends on Application and Infrastructure

**AppHost** (`ObsidianAI.AppHost`)
- .NET Aspire orchestration: starts Web project + MCP Gateway (Docker)
- Service composition and environment configuration

### Critical Patterns

**Reflection Middleware** (`Infrastructure/Middleware/ReflectionFunctionMiddleware.cs`)
- Intercepts destructive MCP operations (delete, patch, move) BEFORE execution
- Calls secondary LLM (`IReflectionService`) to validate safety
- Returns ActionCard JSON if user confirmation needed
- Stores operation context in `IAgentStateService` using reflection key
- User confirms via `/api/actioncards/{key}/confirm` endpoint

**MCP Integration** (`Web/Services/McpClientService.cs`)
- Spawns Node.js MCP server process via `docker mcp gateway` command
- Exposes 9 Obsidian vault tools: list, read, search, create, append, patch, delete, move
- Tool catalog cached for 2 minutes in `McpToolCatalog`
- Tools wrapped with middleware pipeline: `GlobalExceptionHandlingMiddleware` → `ReflectionFunctionMiddleware` → MCP tool
- Configuration fallback: reads MCP_ENDPOINT and MCP_GATEWAY_AUTH_TOKEN from environment variables first, then appsettings
- Requires `~/.config/mcp/mcp.json` with server definitions (Obsidian REST API connection details)

**HttpClient Configuration** (`Web/Program.cs`)
- Uses IHttpContextAccessor for dynamic base URL resolution
- Critical for Blazor Server where Web project hosts both UI and API
- Avoids hardcoded ports - essential with .NET Aspire's dynamic port assignment
- Pattern: `(sp, client) => { var accessor = sp.GetRequiredService<IHttpContextAccessor>(); var request = accessor.HttpContext?.Request; if (request != null) { client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}"); } }`

**Agent Instructions** (`Web/Endpoints/AgentInstructions.cs`)
- System prompt for AI agents
- CRITICAL: Handles attached files (inline in message, DON'T re-read via MCP)
- File resolution: normalize emoji paths (remove emojis, lowercase, trim), match to vault, use original path in tool calls
- Destructive ops: call tool immediately, confirmation automatic via reflection middleware

**Streaming Architecture** (`Web/Hubs/ChatHub.cs`)
- Token batching: buffer 50 chars before flushing to SignalR
- Events: `ReceiveToken`, `StatusUpdate`, `ActionCard`, `Metadata`, `MessageComplete`, `Error`
- SignalR methods: `SendMessage`, `ConfirmActionCard`, `CancelActionCard`, `RegenerateMessage`

**Thread Management** (`Infrastructure/Agents/InMemoryAgentThreadProvider.cs`)
- Each conversation has `ThreadId` for multi-turn context
- Threads stored in-memory (ConcurrentDictionary), NOT persisted
- Microsoft Agents Framework's `AgentThread` maintains message history

**Vault Path Normalization** (`Application/Services/VaultPathResolver.cs`)
- Removes emojis, lowercases, trims spaces, handles `.md` extension
- Caches vault index for 15 seconds
- Returns original emoji path for tool calls

**File Operation Extraction** (`Application/Services/RegexFileOperationExtractor.cs`)
- Parses AI responses to detect file ops mentioned in natural language
- Regex patterns for: created, appended, modified, deleted, moved files
- Extracts action, filepath, content for audit trail

## Key Files

### Configuration & Startup
- `ObsidianAI.AppHost/AppHost.cs` - Aspire orchestration (Web + MCP Gateway), passes MCP_GATEWAY_AUTH_TOKEN to services
- `ObsidianAI.Web/Program.cs` - Service registration, DI, middleware, endpoints, HttpClient with dynamic base URL
- `ObsidianAI.Web/appsettings.json` - LLM provider config, connection strings, MCP configuration fallback
- `.env` - Docker environment variables
- `~/.config/mcp/mcp.json` - MCP gateway server configuration (Obsidian connection details)

### Core Business Logic
- `ObsidianAI.Application/UseCases/StreamChatUseCase.cs` - Main chat orchestrator
- `ObsidianAI.Infrastructure/LLM/BaseChatAgent.cs` - Shared streaming logic for all providers
- `ObsidianAI.Infrastructure/LLM/ConfiguredAIAgentFactory.cs` - Provider selection + agent creation
- `ObsidianAI.Infrastructure/Middleware/ReflectionFunctionMiddleware.cs` - Safety validation
- `ObsidianAI.Infrastructure/LLM/ReflectionPromptBuilder.cs` - Reflection LLM prompt template

### Data Layer
- `ObsidianAI.Infrastructure/Data/ObsidianAIDbContext.cs` - EF Core context
- `ObsidianAI.Infrastructure/Repositories/*Repository.cs` - Data access
- `ObsidianAI.Infrastructure/Migrations/` - EF Core migrations

### UI
- `ObsidianAI.Web/Components/Pages/Chat.razor` - Main chat interface
- `ObsidianAI.Web/Hubs/ChatHub.cs` - SignalR hub for streaming

### Documentation
- `README.md` - User guide
- `SYSTEM-CONTEXT-SUMMARY.md` - Detailed technical reference
- `NEXTJS-MIGRATION-BLUEPRINT.md` - Next.js migration plan (reference only)
- `.github/copilot-instructions.md` - Development conventions
- `.results/5-style-guides/` - Per-category style guides

## Development Workflow

### Adding New Use Case
1. Define interface/contract in `Application/` (if needed)
2. Create use case class in `Application/UseCases/`
3. Register in `Application/DI/ServiceCollectionExtensions.cs`
4. Add endpoint in `Web/Endpoints/` (if REST API)
5. Add Blazor component/SignalR method (if UI feature)
6. Add tests in `ObsidianAI.Tests/`

### Adding New LLM Provider
1. Create client class in `Infrastructure/LLM/` inheriting `BaseChatAgent`
2. Add config POCO in `Infrastructure/Configuration/AppSettings.cs`
3. Update `ConfiguredAIAgentFactory.CreateAgentAsync()` switch statement
4. Add validation in `Web/Program.cs` startup
5. Update health check in `Infrastructure/HealthChecks/LlmHealthCheck.cs`

### Adding New MCP Tool
1. Add tool to MCP server (external Node.js process)
2. Tool auto-discovered via `McpToolCatalog.ListToolsAsync()`
3. Add destructive check in `ReflectionFunctionMiddleware.IsDestructive()` if needed
4. Update agent instructions in `AgentInstructions.cs` if special handling required

### Database Schema Change
1. Modify entity in `Domain/Entities/`
2. Update `ObsidianAIDbContext.OnModelCreating()` if needed
3. Create migration: `dotnet ef migrations add MigrationName --project ObsidianAI.Infrastructure --startup-project ObsidianAI.Web`
4. Review generated migration code
5. Migration applies automatically on app startup (or manually via `dotnet ef database update`)

## Configuration Structure

```json
{
  "LLM": {
    "Provider": "OpenRouter",  // or "LMStudio" or "NanoGPT"
    "LMStudio": {
      "Endpoint": "http://localhost:1234/v1",
      "ApiKey": "lm-studio",  // user secrets
      "Model": "local-model"
    },
    "OpenRouter": {
      "Endpoint": "https://openrouter.ai/api/v1",
      "ApiKey": "",  // user secrets
      "Model": "google/gemini-2.5-flash-lite-preview-09-2025"
    },
    "NanoGPT": {
      "Endpoint": "http://example.com/v1",
      "ApiKey": "",  // user secrets
      "Model": "nano-model"
    }
  },
  "AllowedAttachmentTypes": [".txt", ".md", ".json"],
  "ConnectionStrings": {
    "ObsidianAI": "Data Source=obsidianai.db"
  }
}
```

**Environment Variables**:
- `MCP_ENDPOINT` - MCP gateway URL (fallback to appsettings: `http://localhost:8033/mcp`)
- `MCP_GATEWAY_AUTH_TOKEN` - Auth token for MCP gateway (fallback to appsettings, passed by AppHost)
- `OpenRouter:ApiKey` - OpenRouter API key
- `NanoGpt:ApiKey` - NanoGPT API key
- `LLM__LMStudio__ApiKey` - LMStudio API key

**MCP Gateway Configuration** (`~/.config/mcp/mcp.json`):
```json
{
  "servers": {
    "obsidian": {
      "env": {
        "OBSIDIAN_API_KEY": "your-token-here",
        "OBSIDIAN_HOST": "127.0.0.1",
        "OBSIDIAN_PORT": "27124",
        "OBSIDIAN_HTTPS": "true"
      }
    }
  }
}
```

## Testing Strategy

- **Unit Tests**: Application use cases, domain logic, infrastructure services
- **Test Framework**: xUnit with NSubstitute for mocking
- **Database Testing**: In-memory SQLite provider
- **Test Structure**: Mirrors solution structure in `ObsidianAI.Tests/`

## Important Notes

- **Current Migration Status**: Codebase is .NET/Blazor. `NEXTJS-MIGRATION-BLUEPRINT.md` exists for future migration, but is NOT active.
- **Reflection Prompt**: `ReflectionPromptBuilder.cs` prompt MUST be preserved exactly - critical for safety validation.
- **Attached Files**: If message includes `[ATTACHED FILES]` section, content is inline - DON'T re-read via MCP.
- **Emoji Handling**: Normalize for matching, use original paths in tool calls.
- **Destructive Operations**: Always trigger reflection middleware (delete, patch, move).
- **Token Batching**: 50-char buffer threshold - critical for streaming performance.
- **Thread State**: In-memory only, lost on restart.
- **MCP Server**: Runs as external process via Aspire/Docker, communicates via stdin/stdout JSON-RPC.
- **HttpClient Base URL**: MUST use IHttpContextAccessor for dynamic resolution in Blazor Server apps where Web hosts both UI and API - hardcoded ports fail with Aspire's dynamic port assignment.
- **MCP Configuration**: Requires both application config (MCP_ENDPOINT, MCP_GATEWAY_AUTH_TOKEN) AND gateway config file (`~/.config/mcp/mcp.json`) with Obsidian REST API connection details.
- **Configuration Fallback**: Services read environment variables first, then appsettings - ensures flexibility across deployment environments.

## Conventions

- Use async/await throughout
- CancellationToken on all async methods
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Follow existing patterns in each layer
- Maintain strict dependency direction (outer → inner)
- Register services in layer-specific `ServiceCollectionExtensions`
- Use strongly-typed configuration POCOs
- Persist all file operations to `FileOperationRecord` for audit trail
