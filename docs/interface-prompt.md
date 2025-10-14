```markdown
# Implementation Prompt: Obsidian AI Chat Interface

I need to implement a web-based chat interface for my Obsidian AI Assistant project. I have an HTML prototype that shows the exact design and UX I want. Please help me build this using Blazor Server with SignalR for real-time communication.

## Project Context

**Current Architecture:**
- .NET 9 with Aspire orchestration
- Microsoft Agent Framework coordinating between:
  - LM Studio (localhost:1234) for LLM reasoning
  - Docker MCP Gateway (localhost:8033/mcp) for Obsidian tools
- Existing Minimal API with endpoints:
  - `POST /chat` - Process messages through Agent Framework
  - `POST /vault/search` - Direct vault search
  - `POST /vault/reorganize` - Restructuring operations

**What I Need:**
Add a Blazor Server project to provide the chat UI that connects to my existing API endpoints.

## UI Requirements (Based on Prototype)

### Core Layout
1. **Header**: Fixed top bar with "🧠 Obsidian AI" title and action buttons (Vault, Settings, History)
2. **Chat Area**: Scrollable message feed with user/AI messages
3. **Input Area**: Fixed bottom section with text input, send button, and quick action buttons

### Message Display
- **User messages**: Right-aligned, light gray background (#f0f0f0)
- **AI messages**: Left-aligned, white background with border
- **Avatars**: 36px circular icons (👤 for user, 🤖 for AI)
- **Animations**: Fade-in effect for new messages

### Action Cards (Critical Feature)
For destructive operations (move, delete, reorganize), AI responses must include confirmation cards:

```
┌─────────────────────────────────────────┐
│ 📋 PLANNED ACTIONS                      │
│ • Move Sprint-01-Retro.md → Archive/... │
│ • Move Sprint-02-Retro.md → Archive/... │
│ ... and 4 more · View all               │
│                                         │
│ [✓ Confirm] [✗ Cancel] [Edit Selection] │
└─────────────────────────────────────────┘
```

**Required behavior:**
- Show preview of actions before execution
- Require explicit confirmation
- Update card to show success/cancellation status
- Support expanding to view all items

### Search Results Display
When AI returns search results, render them as cards:
```
┌────────────────────────────────────────┐
│ 📝 Meeting Notes 2024-01-15            │
│ "...discussed project timeline..."     │
│ [Open] [Add to collection]             │
└────────────────────────────────────────┘
```

### Status Indicators
Show real-time feedback during AI processing:
- `🔍 Searching...` (with spinner animation)
- `✏️ Writing...`
- `🔄 Reorganizing...`

### Quick Actions
Below the input field, provide buttons that pre-fill common queries:
- "Search vault"
- "Create note"
- "Reorganize"
- "Summarize"

## Technical Implementation Requirements

### Project Structure
```
ObsidianAI.Web/           # New Blazor Server project
├── Components/
│   ├── Pages/
│   │   └── Chat.razor    # Main chat page
│   ├── Layout/
│   │   └── ChatLayout.razor
│   └── Shared/
│       ├── MessageBubble.razor
│       ├── ActionCard.razor
│       ├── SearchResult.razor
│       └── StatusIndicator.razor
├── Services/
│   ├── IChatService.cs
│   └── ChatService.cs    # API client
├── Models/
│   ├── ChatMessage.cs
│   ├── ActionCardData.cs
│   └── SearchResultData.cs
└── wwwroot/
    └── css/
        └── chat.css      # Based on prototype styles
```

### SignalR Integration
Implement streaming responses from the Agent Framework:

**Hub Implementation:**
```csharp
public class ChatHub : Hub
{
    public async IAsyncEnumerable<string> StreamMessage(string message)
    {
        // Call agent.RunStreamingAsync()
        // Yield each token as it arrives
    }
}
```

**Client-side (Blazor):**
```javascript
connection.stream("StreamMessage", userMessage)
    .subscribe({
        next: (token) => { /* append to UI */ },
        complete: () => { /* finalize message */ }
    });
```

### State Management
- **Conversation history**: Store in memory (list of messages)
- **Session persistence**: Use browser localStorage for conversation recovery
- **Action card state**: Track pending confirmations in component state

### API Integration Patterns

**For standard chat messages:**
```csharp
public async Task<ChatResponse> SendMessageAsync(string message)
{
    var response = await httpClient.PostAsJsonAsync("/chat", 
        new { message });
    return await response.Content.ReadFromJsonAsync<ChatResponse>();
}
```

**For vault search:**
```csharp
public async Task<SearchResponse> SearchVaultAsync(string query)
{
    var response = await httpClient.PostAsJsonAsync("/vault/search",
        new { query });
    return await response.Content.ReadFromJsonAsync<SearchResponse>();
}
```

**For reorganize operations:**
```csharp
public async Task<ReorganizeResponse> ReorganizeAsync(ReorganizeRequest request)
{
    var response = await httpClient.PostAsJsonAsync("/vault/reorganize", 
        request);
    return await response.Content.ReadFromJsonAsync<ReorganizeResponse>();
}
```

### Response Parsing
The AI returns unstructured text. Parse responses to detect:
1. **Action cards**: Look for patterns like "I'll move X files" or "Confirm this action"
2. **Search results**: Extract structured data from search responses
3. **Status updates**: Detect when AI is performing operations

**Example parsing logic:**
```csharp
if (aiResponse.Contains("confirm", StringComparison.OrdinalIgnoreCase))
{
    // Extract planned actions and render ActionCard
}
else if (aiResponse.Contains("found", StringComparison.OrdinalIgnoreCase))
{
    // Extract search results and render SearchResult cards
}
```

### Styling (Based on Prototype)
Use the exact color scheme from the HTML prototype:
- Background: `#f5f5f5`
- Message bubbles: `#ffffff` (AI), `#f0f0f0` (user)
- Borders: `#e0e0e0`, `#d0d0d0`
- Accent color: `#4a7c9d`
- Text: `#2c2c2c`, `#666666` (secondary)

## Critical Features to Implement

### 1. Streaming Response Support
Use `agent.RunStreamingAsync()` from the Agent Framework:
```csharp
await foreach (var token in agent.RunStreamingAsync(message))
{
    await Clients.Caller.SendAsync("ReceiveToken", token);
}
```

### 2. Confirmation Flow for Destructive Actions
Before executing moves/deletes:
1. AI proposes actions
2. Render ActionCard with preview
3. Wait for user confirmation
4. Execute only after confirmation
5. Show success/failure status

### 3. Conversation Context
Maintain conversation history for multi-turn interactions:
```csharp
private List<ChatMessage> conversationHistory = new();

// When sending to Agent Framework, include history
var context = string.Join("\n", conversationHistory.Select(m => 
    $"{m.Role}: {m.Content}"));
```

### 4. Error Handling
Handle common failure scenarios:
- LM Studio offline
- Docker MCP Gateway not running
- Network timeouts
- Invalid vault operations

Display friendly error messages in chat instead of breaking the UI.

## Aspire Integration

Update `ObsidianAI.AppHost/AppHost.cs`:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.ObsidianAI_Api>("api");
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

## Success Criteria

The implementation is complete when:
1. ✅ Chat interface loads and matches prototype design
2. ✅ Messages send to API and receive responses
3. ✅ Streaming responses appear token-by-token
4. ✅ Action cards display for destructive operations
5. ✅ Confirmation flow prevents accidental changes
6. ✅ Search results render as structured cards
7. ✅ Quick actions pre-fill input field
8. ✅ Conversation history persists during session
9. ✅ Status indicators show during AI processing
10. ✅ Error messages display gracefully

## Files to Create

Please generate complete, production-ready code for:
1. `ObsidianAI.Web.csproj` - Project file with dependencies
2. `Program.cs` - Blazor Server setup with SignalR
3. `Components/Pages/Chat.razor` - Main chat page
4. `Components/Shared/MessageBubble.razor` - Message component
5. `Components/Shared/ActionCard.razor` - Confirmation card component
6. `Components/Shared/SearchResult.razor` - Search result component
7. `Services/ChatService.cs` - API client implementation
8. `Hubs/ChatHub.cs` - SignalR hub for streaming
9. `wwwroot/css/chat.css` - Styles from prototype
10. `Models/*.cs` - Required model classes

## Important Notes

- Use `.NET 9` and `Blazor Server` (not Blazor WebAssembly)
- API already exists - just consume the endpoints
- Agent Framework handles tool calling - don't reimplement that logic
- Focus on UI/UX matching the prototype exactly
- Prioritize the confirmation flow - it's critical for safety

Begin by creating the project structure and then implement components in this order: Layout → MessageBubble → ActionCard → Chat page → SignalR streaming.
```