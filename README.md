# ObsidianAI

ObsidianAI is a .NET Aspire application designed to enhance your Obsidian vault experience with AI capabilities. It provides an intelligent assistant that can interact with your Obsidian notes, powered by Large Language Models (LLMs) and the Model Context Protocol (MCP).

## Features

*   **AI-Powered Obsidian Assistant**: Interact with your Obsidian vault using natural language commands.
*   **LLM Integration**: Configurable to use different LLM providers (e.g., LMStudio, OpenRouter).
*   **Model Context Protocol (MCP)**: Enables the AI assistant to use specific tools to manage your Obsidian notes (create, modify, append, delete files).
*   **Real-time Chat Interface**: A Blazor-based web frontend for seamless interaction with the AI assistant.
*   **Conversation History**: Persistent storage of chat sessions with SQLite database and Entity Framework Core.
*   **Conversation Management**: Full CRUD operations for conversations including archive, delete, and export functionality.
*   **Action Cards**: Interactive UI components for confirming file operations before execution.
*   **File Operation Tracking**: Complete audit trail of all file modifications made through the AI assistant.
*   **Clean Architecture**: Enterprise-grade architecture with clear separation of concerns across Domain, Application, Infrastructure, API, and Web layers.
*   **.NET Aspire Orchestration**: Leverages .NET Aspire for simplified development, deployment, and observability of distributed applications.

## Microsoft Agent Framework

### Package Dependencies
The project uses the Microsoft Agent Framework through these NuGet packages:
- **Microsoft.Agents.AI.OpenAI** (v1.0.0-preview.251009.1) - Main framework package
- **Microsoft.Agents.AI** - Core agent functionality
- **Microsoft.Agents.AI.Abstractions** - Framework abstractions

<img width="1398" height="1251" alt="image" src="https://github.com/user-attachments/assets/7faba114-667d-433e-b30c-865c6cb93ca5" />
<img width="1953" height="923" alt="image" src="https://github.com/user-attachments/assets/b01e517b-1be6-43fe-8016-c9b1fc9d8c9c" />

## Key Usage Locations

### 1. Client Factory Implementations
**Files:** [`LmStudioClientFactory.cs`](ObsidianAI.Api/Services/LmStudioClientFactory.cs:1) and [`OpenRouterClientFactory.cs`](ObsidianAI.Api/Services/OpenRouterClientFactory.cs:1)

Both factories use the framework's [`IChatClient`](ObsidianAI.Api/Services/ILlmClientFactory.cs:14) interface and the `.AsIChatClient()` extension method from `Microsoft.Agents.AI` to wrap OpenAI-compatible clients:

```csharp
using Microsoft.Agents.AI;
// ...
return openAIClient.GetChatClient(model).AsIChatClient();
```

### 2. ObsidianAssistantService - Core Agent Implementation
**File:** [`ObsidianAssistantService.cs`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:1)

This is the primary usage location where the Microsoft Agent Framework is leveraged to create AI agents with tool capabilities:

#### Key Framework Usage:
- **Agent Creation** (lines 76-80): Creates a [`ChatClientAgent`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:76) using the framework's `.CreateAIAgent()` extension method
- **Tool Integration** (line 79): Converts MCP tools to [`AITool`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:79) objects that the agent can use
- **Streaming Support** (lines 97-106, 114-168): Uses the framework's streaming capabilities with [`RunAsync()`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:104) and [`RunStreamingAsync()`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:124) methods
- **Function Call Detection** (lines 157-162): Detects and handles [`FunctionCallContent`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:157) from the framework

#### Agent Configuration:
The service creates an agent with:
- **Name**: "ObsidianAssistant"
- **Instructions**: Custom instructions for Obsidian vault management
- **Tools**: MCP tools converted to AITool format for file operations

### 3. Integration Pattern
The framework integrates with:
- **MCP (Model Context Protocol)**: Tools are discovered via MCP and converted to AITool format
- **Multiple LLM Providers**: Works with both LM Studio and OpenRouter through the IChatClient abstraction
- **Streaming Architecture**: Supports real-time streaming responses with incremental updates

## Architecture Benefits
The Microsoft Agent Framework provides:
- **Standardized Agent Interface**: Consistent API for different LLM providers
- **Tool Integration**: Seamless integration of external tools (MCP tools) into agent capabilities
- **Streaming Support**: Built-in streaming capabilities for real-time responses
- **Function Calling**: Native support for detecting and handling tool calls during conversations

## Clean Architecture Implementation

ObsidianAI follows **Clean Architecture** principles with strict dependency direction:

### Layer Dependencies
```
Web → API → Application → Domain ← Infrastructure
```

### Key Architectural Patterns

**Domain Layer (Core Business Logic)**
- Contains only business entities and interfaces
- No external dependencies
- Defines contracts for all external interactions

**Application Layer (Use Cases)**
- Orchestrates business workflows
- Depends only on Domain layer
- Contains DTOs and use case implementations
- Manages transaction boundaries

**Infrastructure Layer (External Concerns)**
- Implements Domain interfaces
- Handles database operations, external APIs, file system
- Contains EF Core DbContext and repository implementations
- Provides LLM client implementations

**API Layer (Web API)**
- Exposes HTTP endpoints for all operations
- Handles streaming with Server-Sent Events
- Integrates with MCP gateway
- Contains health checks and service configuration

**Web Layer (UI)**
- Blazor Server components for interactive UI
- SignalR integration for real-time updates
- Service layer for API communication
- Component-based architecture with shared components

### Data Flow
1. **User Interaction** → Web Layer components
2. **API Calls** → API Layer endpoints
3. **Use Case Execution** → Application Layer
4. **Business Logic** → Domain Layer entities/services
5. **Data Persistence** → Infrastructure Layer repositories
6. **External Integration** → Infrastructure Layer (LLM/MCP)

### Key Benefits
- **Testability**: Each layer can be unit tested in isolation
- **Maintainability**: Clear separation of concerns
- **Flexibility**: Easy to swap implementations (e.g., different LLM providers)
- **Scalability**: Layers can be scaled independently
- **Domain Focus**: Business logic is protected from external concerns


## Project Structure

The solution follows **Clean Architecture** principles with clear separation of concerns across five main layers:

### Core Projects

*   **ObsidianAI.Domain**: Core business entities and abstractions (no external dependencies)
    - Entities: `Conversation`, `Message`, `ActionCardRecord`, `PlannedActionRecord`, `FileOperationRecord`
    - Domain models: `ChatInput`, `ChatStreamEvent`, `FileOperation`, `OperationResult`
    - Ports/Interfaces: `IChatAgent`, `IConversationRepository`, `IMessageRepository`, `IVaultToolExecutor`

*   **ObsidianAI.Application**: Use cases and orchestration between domain and infrastructure
    - Use Cases: `StartChatUseCase`, `StreamChatUseCase`, `CreateConversationUseCase`, `ListConversationsUseCase`, etc.
    - DTOs: `ConversationDto`, `ConversationDetailDto`, `MessageDto`
    - Services: `IMcpClientProvider`, `IFileOperationExtractor`, `IVaultPathNormalizer`

*   **ObsidianAI.Infrastructure**: External concerns and implementations
    - Data: `ObsidianAIDbContext` with Entity Framework Core and SQLite
    - Repositories: `ConversationRepository`, `MessageRepository`
    - LLM Integration: `ConfiguredAIAgentFactory`, `LmStudioChatAgent`, `OpenRouterChatAgent`
    - Vault Operations: `McpVaultToolExecutor`, `NullVaultToolExecutor`
    - Configuration: Strongly-typed settings for LLM providers

*   **ObsidianAI.Api**: Web API endpoints and streaming infrastructure
    - Services: `ObsidianAssistantService`, `McpClientService`, `StreamingEventWriter`
    - Health Checks: LLM and MCP service monitoring
    - Configuration: Service registration and endpoint mapping

*   **ObsidianAI.Web**: Blazor web frontend with real-time communication
    - Components: `Chat.razor`, `ConversationSidebar.razor`, `ChatArea.razor`, `ActionCard.razor`
    - Services: `ChatService`, `IChatService`, SignalR `ChatHub`
    - Models: `ChatMessage`, `ConversationMetadata`, `ActionCardData`, `FileOperationData`

### Orchestration Projects

*   **ObsidianAI.AppHost**: The .NET Aspire host project that orchestrates all services
*   **ObsidianAI.ServiceDefaults**: Shared configurations and extensions for .NET Aspire services
*   **ObsidianAI.Tests**: Unit and integration tests for application and infrastructure layers

## Getting Started

### Prerequisites

*   .NET 8 SDK
*   Docker (for running LMStudio or other containerized LLMs, if applicable)
*   An Obsidian vault (for the AI assistant to interact with)
*   MCP Gateway (Docker container automatically started by Aspire)

### Running the Application

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/your-repo/ObsidianAI.git
    cd ObsidianAI
    ```

2.  **Configure LLM Provider**:
    In `ObsidianAI.Api/appsettings.json` (or `appsettings.Development.json`), configure your preferred LLM provider.
    
    Example for LMStudio:
    ```json
    "LLM": {
      "Provider": "LMStudio",
      "LMStudio": {
        "BaseUrl": "http://localhost:1234/v1",
        "Model": "your-lmstudio-model-name"
      }
    }
    ```
    
    Example for OpenRouter:
    ```json
    "LLM": {
      "Provider": "OpenRouter",
      "OpenRouter": {
        "BaseUrl": "https://openrouter.ai/api/v1",
        "ApiKey": "YOUR_OPENROUTER_API_KEY",
        "Model": "your-openrouter-model-name"
      }
    }
    ```

3.  **Configure Database Connection** (optional):
    The application uses SQLite by default with a local database file (`obsidianai.db`). You can customize the connection string:
    ```json
    "ConnectionStrings": {
      "ObsidianAI": "Data Source=obsidianai.db"
    }
    ```

4.  **Run the Aspire AppHost**:
    ```bash
    dotnet run --project ObsidianAI.AppHost
    ```
    This will launch the .NET Aspire dashboard, from which you can access the Web frontend and API. The database will be automatically created and migrated on first run.

## Docker & Local Deployment

ObsidianAI can be run locally using Docker and docker-compose. This setup orchestrates the API, Web UI, and MCP Gateway, and persists chat data in a local SQLite volume.

### Prerequisites
- Docker Desktop (Windows/Mac/Linux)
- .NET 9 SDK (for local builds)

### Quick Start
1. **Clone the repository**
2. **Configure environment variables**
   - Copy `.env` (created by default) and set your LLM API keys and endpoints as needed.
3. **Build and run the stack:**
   ```powershell
   docker compose up --build
   ```
4. **Access the app:**
   - Web UI: [http://localhost:5244](http://localhost:5244)
   - API: [http://localhost:5095](http://localhost:5095)
   - MCP Gateway: [http://localhost:8033](http://localhost:8033)

### Data Persistence
- All chat and conversation data is stored in a SQLite file inside a Docker volume (`obsidianai-data`).
- Data is retained across container restarts.

### Environment Variables
- LLM provider keys and endpoints are set via `.env` and injected into containers at runtime.
- Example `.env`:
  ```env
  LLM__Provider=OpenRouter
  LLM__LMStudio__Endpoint=http://localhost:1234/v1
  LLM__LMStudio__ApiKey=lm-studio
  LLM__LMStudio__Model=openai/gpt-oss-20b
  LLM__OpenRouter__Endpoint=https://openrouter.ai/api/v1
  LLM__OpenRouter__ApiKey=sk-or-v1-xxxx
  LLM__OpenRouter__Model=google/gemini-2.5-flash-lite-preview-09-2025
  ```

### MCP Gateway
- The MCP Gateway is orchestrated as a container and exposed to the API via the internal Docker network.
- The API uses the environment variable `MCP_ENDPOINT` to connect to the gateway.

### Database Migrations
- The API automatically applies EF Core migrations on startup. No manual migration steps are required.

### Stopping & Restarting
- To stop the stack:
  ```powershell
  docker compose down
  ```
- To restart and preserve data:
  ```powershell
  docker compose up
  ```

### Troubleshooting
- Ensure your LLM API keys are valid and endpoints are reachable from within Docker.
- For LM Studio, you may need to expose the service to Docker or use OpenRouter for cloud-based inference.

---

## Usage

Once the application is running, navigate to the ObsidianAI.Web service in the Aspire dashboard. You can then use the chat interface to interact with your Obsidian vault.

### Basic Chat Commands

*   "Create a new note called 'Meeting Minutes' with content 'Discussed project roadmap.'"
*   "Append 'Action items: Follow up with John' to 'Meeting Minutes'."
*   "List all files in my vault."
*   "Search for 'project roadmap' in my notes."

### Conversation Management

The application now includes full conversation history management:

*   **Conversation History**: Access all previous chat sessions through the history sidebar
*   **New Conversations**: Start fresh chat sessions at any time
*   **Conversation Search**: Find specific conversations by title or content
*   **Archive/Delete**: Manage your conversation library with archive and delete options
*   **Export**: Export conversations as JSON for backup or sharing
*   **Persistent Storage**: All conversations are automatically saved to SQLite database

### Action Cards & File Operations

When the AI assistant proposes file modifications, you'll see interactive **Action Cards** that allow you to:

*   **Review**: See exactly what changes will be made before execution
*   **Confirm**: Approve or cancel specific file operations
*   **Track**: Monitor the status of completed operations
*   **Audit**: View complete history of all file modifications

### URL Navigation

You can directly access specific conversations using URL parameters:
```
https://localhost:port/?conversationId=your-conversation-guid
```

## Database Schema

The application uses SQLite with Entity Framework Core for data persistence. The database includes the following main tables:

### Core Tables

**Conversations**
- Stores chat session metadata (title, created/updated dates, provider info)
- Supports archiving and multi-user scenarios (future)

**Messages**
- Individual chat messages with role (User/Assistant/System)
- Links to conversations with proper ordering
- Supports processing status and token tracking

**ActionCards**
- Interactive action proposals from the AI assistant
- Tracks status (Pending/Processing/Completed/Failed/Cancelled)
- Contains planned actions for user confirmation

**PlannedActions**
- Individual file operations within action cards
- Supports create, modify, move, delete operations
- Includes source/destination paths and content

**FileOperations**
- Completed file operation audit trail
- Tracks actual executed operations
- Links back to original messages

### Database Migrations

The application uses Entity Framework Core migrations. Initial migration is automatically applied on startup:
```bash
dotnet ef database update
```

For development, you can create new migrations:
```bash
dotnet ef migrations add MigrationName --project ObsidianAI.Infrastructure
```

## Development

### Project Dependencies

**Key NuGet Packages**:
- `Microsoft.EntityFrameworkCore.Sqlite` (9.x) - Database provider
- `Microsoft.Agents.AI.OpenAI` (v1.0.0-preview.251009.1) - Agent framework
- `Microsoft.AspNetCore.SignalR` - Real-time communication
- `Microsoft.Extensions.Hosting` - Application hosting
- `Aspire.Hosting` - Service orchestration

### Testing

The solution includes comprehensive test coverage:
- **Unit Tests**: Application layer use cases and infrastructure repositories
- **Integration Tests**: API endpoints and database operations
- **Test Framework**: xUnit with in-memory SQLite for testing

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test ObsidianAI.Tests
```

### Configuration

**Development Environment**:
- Use `appsettings.Development.json` for local configuration
- Database file is created automatically in the API project directory
- MCP gateway runs as Docker container on localhost:8033

**Production Considerations**:
- Configure appropriate connection strings for persistent storage
- Set up proper logging and monitoring
- Consider user authentication for multi-tenant scenarios
- Implement backup strategies for conversation data

## Contributing

Contributions are welcome! Please refer to the `CONTRIBUTING.md` file (if available) for guidelines.

### Development Guidelines
- Follow Clean Architecture principles
- Maintain separation of concerns between layers
- Write unit tests for new use cases and infrastructure components
- Update documentation for new features
- Use dependency injection throughout the application

## License

This project is licensed under the MIT License.
