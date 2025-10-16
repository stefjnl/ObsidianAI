# Tech Stack & Domain Summary

_This file mirrors `.results/1-determine-techstack.md` to satisfy downstream prompts that expect `1-techstack.md`._

## Core Technology Analysis
- **Programming languages:** C# across all application layers; Razor (.razor) markup for Blazor components; limited JSON/YAML for configuration; Docker Compose YAML for container orchestration.
- **Primary framework:** .NET 8 with ASP.NET Core. The API is an ASP.NET Core Web API service; the UI is a Blazor Server app.
- **Secondary frameworks & libraries:**
  - .NET Aspire for distributed application orchestration (`ObsidianAI.AppHost`).
  - Entity Framework Core (SQLite provider) for persistence in `ObsidianAI.Infrastructure`.
  - Microsoft Agent Framework (`Microsoft.Agents.AI.*`) and Microsoft.Extensions.AI abstractions for LLM agent construction.
  - SignalR for realtime streaming between API and Blazor frontend.
  - Model Context Protocol (MCP) gateway executable managed by Aspire for tool discovery.
- **State management approach:**
  - Server-side state handled via Entity Framework Core repositories and CQRS-style use cases in the Application layer.
  - Blazor Server maintains UI state on the server; SignalR hub (`ChatHub`) coordinates live chat updates.
  - Conversations, messages, and action cards persisted in SQLite with repositories implementing domain ports.
- **Other notable technologies & patterns:**
  - Clean Architecture layering: Domain, Application, Infrastructure, API, Web with ServiceDefaults and AppHost orchestration.
  - Dependency Injection per ASP.NET Core conventions; strongly-typed options for LLM providers.
  - Streaming Server-Sent Events from the API via custom `StreamingEventWriter`.
  - Docker Compose for local orchestration of API, Web, and MCP gateway.

## Domain Specificity Analysis
- **Problem domain:** Augmenting an Obsidian vault with an AI assistant capable of reading, managing, and editing vault notes through natural language conversations.
- **Core business concepts:** Conversation lifecycle management, assistant messages, action cards representing proposed vault file operations, planned vs executed file operations, MCP tool execution, and LLM provider selection.
- **Supported user interactions:**
  - Initiating and streaming chat conversations with the assistant.
  - Reviewing conversation history, archiving, deleting, and exporting chats.
  - Confirming or rejecting AI-proposed file operations via action cards.
  - Observing realtime assistant responses in the web UI.
- **Primary data types & structures:**
  - Domain entities (`Conversation`, `Message`, `ActionCardRecord`, `PlannedActionRecord`, `FileOperationRecord`).
  - DTOs and Use Cases in `ObsidianAI.Application` (e.g., `ConversationDto`, `StartChatUseCase`).
  - API models (`ChatRequest`, streaming `ChatMessage`).
  - Blazor UI models (`ChatMessage`, `ConversationMetadata`, `ActionCardData`).
  - Configuration models for LLM providers and MCP endpoints.

## Application Boundaries
- **Clearly in-scope functionality:**
  - LLM-backed chat flow with streaming responses.
  - MCP tool discovery and invocation for vault file operations.
  - Conversation history persistence, CRUD, and export.
  - Blazor Server frontend with SignalR-driven realtime updates.
  - .NET Aspire-based orchestration including MCP gateway process management.
  - Health checks and logging for LLM connectivity.
- **Architecturally inconsistent features:**
  - Client-heavy SPA frameworks (React/Vue) or REST clients bypassing the established Blazor Server + SignalR architecture.
  - Direct database access from UI or API bypassing Application layer use cases.
  - Alternative persistence technologies (e.g., MongoDB, Cosmos DB) without aligning with EF Core repositories.
  - LLM integrations that ignore the `ILlmClientFactory` abstraction or Microsoft Agent Framework pipeline.
  - Non-MCP-based tool invocation patterns or ad-hoc filesystem access bypassing Vault tool executors.
- **Specialized libraries / domain constraints:**
  - Microsoft Agent Framework dictates agent creation and tool invocation flow.
  - MCP tooling restricts how file system operations are surfaced to the assistant.
  - Entity Framework Core with SQLite sets expectations for relational schema and migrations.
  - .NET Aspire orchestrates service startup order and environment configuration, implying features should integrate via its resource model.
