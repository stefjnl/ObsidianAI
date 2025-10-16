# UI Domain Deep Dive

The Blazor Server frontend (`ObsidianAI.Web`) delivers the chat experience, conversation management, and vault browsing. Components are server-side interactive, subscribe to SignalR updates, and call the API through typed services.

## Key Conventions
- **Interactive server rendering:** Pages such as `Components/Pages/Chat.razor` declare `@rendermode InteractiveServer`, ensuring persistent SignalR connections and server-side state.
- **Service injection:** Components rely on DI to obtain `IChatService`, `NavigationManager`, `ILogger<T>`, and other helpers; direct `HttpClient` usage inside components is avoided.
- **Model-driven UI:** UI state is represented with records in `ObsidianAI.Web/Models`, and components exchange these models rather than untyped dictionaries.
- **State updates via InvokeAsync:** Streaming callbacks use `InvokeAsync` / `StateHasChanged` to safely mutate component state while observing SignalR threading rules.

## Representative Code
### Component bootstrap (`Chat.razor`)
```razor
@page "/"
@rendermode InteractiveServer
@implements IAsyncDisposable
@using ObsidianAI.Web.Models
@using ObsidianAI.Web.Services
@using ObsidianAI.Web.Components.Shared
@using Microsoft.AspNetCore.SignalR.Client
@using System.Text.RegularExpressions
@using System.Text.Json
@using Microsoft.Extensions.Logging
@using Microsoft.AspNetCore.WebUtilities
@using System.IO
@using System.Linq
@inject IChatService ChatService
@inject NavigationManager NavigationManager
@inject IJSRuntime jsRuntime
@inject ILogger<Chat> Logger
```

### Building and handling a SignalR connection
```csharp
private async Task InitializeSignalR()
{
    hubConnection = new HubConnectionBuilder()
        .WithUrl(NavigationManager.ToAbsoluteUri("/chathub"))
        .WithAutomaticReconnect()
        .Build();

    hubConnection.On<string>("ReceiveToken", (token) =>
    {
        InvokeAsync(() =>
        {
            if (currentAiMessage != null)
            {
                var index = conversationHistory.FindLastIndex(m => m.ClientId == currentAiMessage.ClientId);
                if (index >= 0)
                {
                    currentAiMessage = currentAiMessage with
                    {
                        Content = currentAiMessage.Content + token,
                        IsProcessing = false
                    };

                    conversationHistory[index] = currentAiMessage;

                    tokenBatchCount++;
                    if (tokenBatchCount >= TokenBatchSize)
                    {
                        tokenBatchCount = 0;
                        try
                        {
                            StateHasChanged();
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
            }
        });
    });

    await hubConnection.StartAsync();
}
```

## Implementation Notes
- **Composition-first layout:** `ChatLayout.razor` and shared components (`ConversationSidebar`, `ActionCard`) structure the interface while delegating behavior back to the page-level state container.
- **Optimistic updates:** When sending messages, the component injects optimistic `ChatMessage` records with temporary IDs, later promoted using streaming metadata from SignalR.
- **Vault integration:** `VaultBrowser.razor` triggers chat actions by inserting canonical commands such as `"Read the file "{path}""`, ensuring consistent prompts.
- **Client helpers:** `TextDecoderService` repairs surrogate pairs in streamed text so the UI can render partial Unicode tokens without glitches.
- **Styling:** Global chat styles live under `wwwroot/css/chat.css` and component-specific styles like `MainLayout.razor.css` accompany their Razor files.
