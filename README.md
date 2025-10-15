<img width="1398" height="1251" alt="image" src="https://github.com/user-attachments/assets/7faba114-667d-433e-b30c-865c6cb93ca5" />

# ObsidianAI

ObsidianAI is a .NET Aspire application designed to enhance your Obsidian vault experience with AI capabilities. It provides an intelligent assistant that can interact with your Obsidian notes, powered by Large Language Models (LLMs) and the Model Context Protocol (MCP).

## Features

*   **AI-Powered Obsidian Assistant**: Interact with your Obsidian vault using natural language commands.
*   **LLM Integration**: Configurable to use different LLM providers (e.g., LMStudio, OpenRouter).
*   **Model Context Protocol (MCP)**: Enables the AI assistant to use specific tools to manage your Obsidian notes (create, modify, append, delete files).
*   **Real-time Chat Interface**: A Blazor-based web frontend for seamless interaction with the AI assistant.
*   **.NET Aspire Orchestration**: Leverages .NET Aspire for simplified development, deployment, and observability of distributed applications.

## Microsoft Agent Framework


### Package Dependencies
The project uses the Microsoft Agent Framework through these NuGet packages:
- **Microsoft.Agents.AI.OpenAI** (v1.0.0-preview.251009.1) - Main framework package
- **Microsoft.Agents.AI** - Core agent functionality
- **Microsoft.Agents.AI.Abstractions** - Framework abstractions

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


## Project Structure

The solution is composed of three main projects:

*   **ObsidianAI.AppHost**: The .NET Aspire host project that orchestrates the API and Web projects.
*   **ObsidianAI.Api**: A C# ASP.NET Core API that exposes endpoints for chat interactions, vault modifications, and LLM provider configuration. It integrates with LLMs and the MCP client.
*   **ObsidianAI.Web**: A Blazor web application that provides the user interface for interacting with the AI assistant. It communicates with the API via HTTP and SignalR for real-time updates.
*   **ObsidianAI.ServiceDefaults**: A shared project containing common configurations and extensions for .NET Aspire services.

## Getting Started

### Prerequisites

*   .NET 8 SDK
*   Docker (for running LMStudio or other containerized LLMs, if applicable)
*   An Obsidian vault (for the AI assistant to interact with)

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
3.  **Run the Aspire AppHost**:
    ```bash
    dotnet run --project ObsidianAI.AppHost
    ```
    This will launch the .NET Aspire dashboard, from which you can access the Web frontend and API.

## Usage

Once the application is running, navigate to the ObsidianAI.Web service in the Aspire dashboard. You can then use the chat interface to interact with your Obsidian vault.

Example commands:
*   "Create a new note called 'Meeting Minutes' with content 'Discussed project roadmap.'"
*   "Append 'Action items: Follow up with John' to 'Meeting Minutes'."
*   "List all files in my vault."
*   "Search for 'project roadmap' in my notes."

## Contributing

Contributions are welcome! Please refer to the `CONTRIBUTING.md` file (if available) for guidelines.

## License

This project is licensed under the MIT License.