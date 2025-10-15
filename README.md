<img width="1398" height="1251" alt="image" src="https://github.com/user-attachments/assets/7faba114-667d-433e-b30c-865c6cb93ca5" />

# ObsidianAI

ObsidianAI is a .NET Aspire application designed to enhance your Obsidian vault experience with AI capabilities. It provides an intelligent assistant that can interact with your Obsidian notes, powered by Large Language Models (LLMs) and the Model Context Protocol (MCP).

## Features

*   **AI-Powered Obsidian Assistant**: Interact with your Obsidian vault using natural language commands.
*   **LLM Integration**: Configurable to use different LLM providers (e.g., LMStudio, OpenRouter).
*   **Model Context Protocol (MCP)**: Enables the AI assistant to use specific tools to manage your Obsidian notes (create, modify, append, delete files).
*   **Real-time Chat Interface**: A Blazor-based web frontend for seamless interaction with the AI assistant.
*   **.NET Aspire Orchestration**: Leverages .NET Aspire for simplified development, deployment, and observability of distributed applications.

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