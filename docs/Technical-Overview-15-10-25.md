# Detailed Technical Overview of ObsidianAI

## Architecture Overview

ObsidianAI is built using **Clean Architecture** principles with clear separation of concerns across four primary layers:

### 1. Domain Layer (`ObsidianAI.Domain`)
- **Purpose**: Core business entities and abstractions
- **Key Components**:
  - [`IChatAgent`](ObsidianAI.Domain/Ports/IChatAgent.cs:11): Provider-agnostic chat agent interface
  - [`IVaultToolExecutor`](ObsidianAI.Domain/Ports/IVaultToolExecutor.cs:1): Abstraction for vault operations
  - [`ChatStreamEvent`](ObsidianAI.Domain/Models/ChatStreamEvent.cs:25): Streaming event model with text/tool call events
  - [`FileOperation`](ObsidianAI.Domain/Models/FileOperation.cs:1): File operation domain models

### 2. Application Layer (`ObsidianAI.Application`)
- **Purpose**: Use cases and orchestration between domain and infrastructure
- **Key Components**:
  - [`StartChatUseCase`](ObsidianAI.Application/UseCases/StartChatUseCase.cs:6): Handles non-streaming chat interactions
  - [`StreamChatUseCase`](ObsidianAI.Application/UseCases/StreamChatUseCase.cs:6): Manages streaming chat with tool integration
  - [`SearchVaultUseCase`](ObsidianAI.Application/UseCases/SearchVaultUseCase.cs:7): Vault search functionality (placeholder)
  - [`ModifyVaultUseCase`](ObsidianAI.Application/UseCases/ModifyVaultUseCase.cs:6): File operations orchestration
  - [`IMcpClientProvider`](ObsidianAI.Application/Services/IMcpClientProvider.cs:10): MCP client abstraction

### 3. Infrastructure Layer (`ObsidianAI.Infrastructure`)
- **Purpose**: External concerns and implementations
- **Key Components**:
  - [`ConfiguredAIAgentFactory`](ObsidianAI.Infrastructure/LLM/ConfiguredAIAgentFactory.cs:11): Provider-agnostic agent factory
  - [`LmStudioChatAgent`](ObsidianAI.Infrastructure/LLM/LmStudioChatAgent.cs:17): Local LLM implementation
  - [`OpenRouterChatAgent`](ObsidianAI.Infrastructure/LLM/OpenRouterChatAgent.cs:17): Cloud LLM implementation
  - [`McpVaultToolExecutor`](ObsidianAI.Infrastructure/Vault/McpVaultToolExecutor.cs:17): MCP tool execution
  - Configuration classes for LLM providers

### 4. API Layer (`ObsidianAI.Api`)
- **Purpose**: Web API endpoints and streaming infrastructure
- **Key Components**:
  - [`ObsidianAssistantService`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:21): Core service integrating LLM + MCP
  - [`StreamingEventWriter`](ObsidianAI.Api/Streaming/StreamingEventWriter.cs:11): Server-Sent Events implementation
  - [`McpClientService`](ObsidianAI.Api/Services/McpClientService.cs:14): MCP client lifecycle management
  - Health checks for LLM and MCP services

### 5. Web Layer (`ObsidianAI.Web`)
- **Purpose**: Blazor web frontend with real-time communication
- **Key Components**:
  - [`ChatHub`](ObsidianAI.Web/Hubs/ChatHub.cs:11): SignalR hub for real-time streaming
  - [`Chat.razor`](ObsidianAI.Web/Components/Pages/Chat.razor:1): Main chat interface component
  - [`ChatService`](ObsidianAI.Web/Services/ChatService.cs:10): API communication service

## Core Technical Implementation

### 1. Microsoft Agent Framework Integration

The application leverages the **Microsoft Agent Framework** for AI orchestration:

```csharp
// Agent creation with tools
_agent = _chatClient.CreateAIAgent(
    name: "ObsidianAssistant",
    instructions: _instructions,
    tools: [.. tools.Cast<AITool>()]
);
```

- **Tool Integration**: MCP tools are converted to [`AITool`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:79) objects
- **Streaming Support**: Uses [`RunStreamingAsync()`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:124) for real-time responses
- **Function Calling**: Detects [`FunctionCallContent`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:157) for tool execution

### 2. LLM Provider Abstraction

**Factory Pattern Implementation**:
- [`ILlmClientFactory`](ObsidianAI.Api/Services/ILlmClientFactory.cs:1) abstraction for different providers
- [`LmStudioClientFactory`](ObsidianAI.Api/Services/LmStudioClientFactory.cs:1) for local models
- [`OpenRouterClientFactory`](ObsidianAI.Api/Services/OpenRouterClientFactory.cs:1) for cloud models

**Agent Implementation**:
- Both providers implement [`IChatAgent`](ObsidianAI.Domain/Ports/IChatAgent.cs:11) interface
- Use Microsoft's [`IChatClient.AsIChatClient()`](ObsidianAI.Infrastructure/LLM/OpenRouterChatAgent.cs:37) extension
- Support both synchronous and streaming operations

### 3. Model Context Protocol (MCP) Integration

**MCP Gateway**:
- Docker container running on `localhost:8033`
- Exposes 116+ tools for vault operations
- Tools discovered dynamically via [`ListToolsAsync()`](ObsidianAI.Api/Services/ObsidianAssistantService.cs:79)

**Tool Execution**:
- [`McpVaultToolExecutor`](ObsidianAI.Infrastructure/Vault/McpVaultToolExecutor.cs:17) maps domain operations to MCP tools:
  - `obsidian_append_content`
  - `obsidian_patch_content`
  - `obsidian_delete_file`
  - `obsidian_create_file`

### 4. Streaming Architecture

**Server-Sent Events (SSE)**:
- [`StreamingEventWriter`](ObsidianAI.Api/Streaming/StreamingEventWriter.cs:11) handles SSE formatting
- Supports both text chunks and tool call events
- Proper error handling and completion signaling

**SignalR Integration**:
- [`ChatHub`](ObsidianAI.Web/Hubs/ChatHub.cs:11) bridges SSE to SignalR
- Token batching for performance (3-token batches)
- Surrogate pair handling for Unicode characters
- Connection lifecycle management

### 5. .NET Aspire Orchestration

**Service Composition**:
```csharp
var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", ...);
var api = builder.AddProject<Projects.ObsidianAI_Api>("api")
    .WithEnvironment("MCP_ENDPOINT", "http://localhost:8033/mcp")
    .WaitFor(mcpGateway);
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithReference(api);
```

**Service Discovery**:
- Automatic endpoint resolution via Aspire
- Health checks for all services
- Centralized configuration management

## Key Technical Patterns

### 1. Clean Architecture Compliance
- **Dependency Inversion**: Domain layer defines all interfaces
- **Single Responsibility**: Each class has one clear purpose
- **Open/Closed**: Extensible through interfaces and abstractions

### 2. Dependency Injection
- Proper service lifetime management
- Factory patterns for conditional object creation
- Options pattern for configuration

### 3. Asynchronous Programming
- Full async/await throughout the stack
- Cancellation token propagation
- IAsyncEnumerable for streaming

### 4. Error Handling
- Comprehensive exception handling at all layers
- Graceful degradation when MCP is unavailable
- Health checks for service monitoring

### 5. Performance Optimizations
- Token batching in SignalR (3-token chunks)
- Streaming responses for perceived performance
- Lazy initialization of expensive resources

## Configuration Management

**Strongly-Typed Configuration**:
- [`AppSettings`](ObsidianAI.Infrastructure/Configuration/AppSettings.cs:6) root configuration
- [`LlmSettings`](ObsidianAI.Infrastructure/Configuration/LlmSettings.cs:1) for LLM providers
- Provider-specific settings for LM Studio and OpenRouter

**Environment Variables**:
- `MCP_ENDPOINT` for MCP gateway location
- Aspire service discovery for internal communication
- Development vs. production configuration handling