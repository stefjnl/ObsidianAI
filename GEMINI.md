# ObsidianAI System Context

ObsidianAI is a **production-grade .NET 9 Distributed Application** designed to enhance the Obsidian vault experience through agentic AI. It completely decouples the *intelligence* (LLM) from the *execution* (File System actions) using the **Model Context Protocol (MCP)**.

## Core Identity & Architecture

*   **Pattern**: Clean Architecture (Domain $\rightarrow$ Application $\rightarrow$ Infrastructure $\rightarrow$ Web/API) with .NET Aspire orchestration.
*   **Agentic Model**: Tool-use loops (ReAct pattern) powered by `Microsoft.Agents.AI`, delegating *all* physical side-effects to an MCP Server.
*   **Safety Layer**: A custom `ReflectionFunctionMiddleware` intercepts destructive tool calls (delete/move/overwrite), requiring a "Thought/Reflection" step and user confirmation before execution.

## 1. Technical Stack (Current)

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Orchestrator** | .NET Aspire | Composes API, Web, and MCP sidecars. |
| **Frontend** | Blazor Server (.NET 9) | Real-time chat UI, uses SignalR for token streaming. |
| **Backend** | ASP.NET Core Web API | Hosts the Agent logic and REST endpoints. |
| **Database** | SQLite + EF Core | Persists conversations and messages. |
| **Tooling Protocol** | **MCP (Model Context Protocol)** | Standardizes discovery and execution of vault tools. |
| **MCP Server** | Node.js / Docker | Official `obsidian-mcp` (or similar) running in a sidecar. |
| **LLM Clients** | OpenAI Compatible | **NanoGPT** (Default), OpenRouter, LM Studio. |

## 2. Agentic Architecture & Data Flow

The system does *not* directly touch files. It uses a strict delegation chain:

1.  **User Intent**: User asks "Create a note about quantum physics."
2.  **Agent Logic** (`ObsidianAssistantService`):
    *   Consults `LlmProviderRuntimeStore` for the active LLM.
    *   Generates a tool call: `create_note(path="Quantum Physics.md", content="...")`.
3.  **MCP Interception**:
    *   The MCP Client (`McpClientService`) receives the tool call.
    *   **Reflection Check**: If the action is destructive, `ReflectionFunctionMiddleware` halts execution and sends an `ActionCard` to the UI.
4.  **Execution via Gateway**:
    *   Once validated, the request is sent to the **MCP Gateway** (Docker sidecar or Host process).
    *   The Gateway routes it to the specific **Obsidian MCP Server**.
5.  **Physical Effect**: The MCP Server writes the file to the actual release vault.

## 3. Deployment & Network Topology (Critical)

The deployment has specific networking and configuration patterns to handle communication in both Docker and Aspire environments.

### HttpClient Configuration (Aspire/Local)
*   **Dynamic Base URL**: HttpClient uses `IHttpContextAccessor` to resolve base URL from current request
*   **Why**: Web project hosts both Blazor UI and REST API - no separate API server
*   **Pattern**: `client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}")`
*   **Aspire Impact**: Dynamic port assignment means hardcoded ports (like `localhost:8080`) fail
*   **Fallback**: If HttpContext unavailable (e.g., startup), uses `ApiBaseUrl` from configuration

### Docker Deployment
*   **Internal Port (Container Network)**: `8080`.
    *   Used for *inter-service* communication.
    *   Used for *SignalR Loopback* (Blazor connecting to its own Hub).
*   **External Port (Host Network)**: `5244`.
    *   Exposed only for the Browser to access the UI.
    *   **NEVER** use this port for internal container API calls (results in `Connection refused`).

### `docker-compose.yml` Configuration
*   **`extra_hosts`**: Maps `host.docker.internal` to the host gateway, allowing the container to talk to the MCP Gateway running on the host.
*   **Environment Variables**:
    *   `ApiBaseUrl`: Controls the internal API target (fallback only).
    *   `NanoGpt__ApiKey`: Maps the secret key.
    *   `MCP_ENDPOINT`: Points to the Gateway (e.g., `http://localhost:8033/mcp`).
    *   `MCP_GATEWAY_AUTH_TOKEN`: Auth token for gateway (passed from AppHost to services).

## 4. Configuration & Runtime State

*   **Default Provider**: **NanoGPT** (Model: `zai-org/glm-4.7`).
*   **Runtime Switching**: The `LlmProviderRuntimeStore` allows switching providers (e.g., to OpenRouter or local LM Studio) *without* restarting the application.
*   **Configuration Fallback Pattern**: Environment variables take precedence over appsettings.json
    *   `McpClientService`: Reads `MCP_ENDPOINT` and `MCP_GATEWAY_AUTH_TOKEN` from env first, then config
    *   Enables flexible deployment across local/Docker/cloud without code changes
*   **MCP Gateway Setup**: Requires `~/.config/mcp/mcp.json` with server definitions
    ```json
    {
      "servers": {
        "obsidian": {
          "env": {
            "OBSIDIAN_API_KEY": "your-token",
            "OBSIDIAN_HOST": "127.0.0.1",
            "OBSIDIAN_PORT": "27124",
            "OBSIDIAN_HTTPS": "true"
          }
        }
      }
    }
    ```
*   **Key Files**:
    *   `ObsidianAI.Web/Program.cs`: Global DI and HttpClient configuration (dynamic base URL via IHttpContextAccessor).
    *   `ObsidianAI.Web/Components/Pages/Chat.razor`: The brain of the UI (Streaming, Action Cards, SignalR).
    *   `ObsidianAI.Infrastructure/LLM/LlmProviderRuntimeStore.cs`: Dynamic provider management.
    *   `ObsidianAI.Web/Services/McpClientService.cs`: MCP client initialization with config fallback.
    *   `ObsidianAI.AppHost/AppHost.cs`: Passes MCP_GATEWAY_AUTH_TOKEN to services.
    *   `MCP-VAULT-CONNECTION-STATUS.md`: The definitive log of the connectivity setup.

## 5. Development Guidelines (Antigravity Style)

1.  **Respect the Architecture**: Do not put business logic in the UI (`.razor`). Push it to `Application` (Use Cases) or `Infrastructure` (Services).
2.  **Verify MCP Connectivity**: Always assume the MCP connection might be fragile throughout code changes. Check `McpHealthCheck`.
3.  **UI/UX**: The UI aims for a "Premium" feel. Use the established CSS variables and avoid generic styling.
4.  **Migration Awareness**: While creating new features, keep the *Next.js Migration Blueprint* in mind. Avoid logic that relies heavily on server-side .NET state (like `CircuitHandler`) if it can be stateless REST.

## 6. Migration Context
*   **Blueprint**: `NEXTJS-MIGRATION-BLUEPRINT.md` details the future move to Next.js 15.
*   **Strategy**: Current .NET components are being built to strictly separate UI from Logic to facilitate this future port.

---
*Last Updated: 2025-12-30 (Post-Fix: SignalR Loopback & NanoGPT Default)*
