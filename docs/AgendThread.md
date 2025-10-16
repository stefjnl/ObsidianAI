# Implementation Plan: Multi-Turn Conversation with AgentThread

see: https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/multi-turn-conversation?pivots=programming-language-csharp



After reading the Microsoft documentation, `AgentThread` is the perfect fit for managing conversation history in ObsidianAI. Here's why:

### Current Gap
The application currently:
- ✅ Persists conversations to SQLite
- ✅ Uses `ChatClientAgent` for single-turn interactions
- ❌ **Doesn't pass conversation history to the agent**
- ❌ Relies on agent re-initialization each time (stateless)

### What AgentThread Provides
- **Persistent conversation context** - Maintains message history across multiple turns
- **Automatic state management** - Handles conversation lifecycle
- **Built-in history tracking** - No manual message tracking needed
- **Tool call history** - Preserves tool invocations across turns
- **Streaming support** - Works with `RunStreamingAsync()`

---

## Implementation Plan

### **Phase 1: Domain Layer Changes** (30 min)

#### 1.1 Add Thread Identifier to Conversation Entity
**Location**: `ObsidianAI.Domain/Entities/Conversation.cs`

**Change**:
```csharp
public class Conversation
{
    // Existing properties...
    
    /// <summary>
    /// AgentThread identifier for managing multi-turn conversation history.
    /// </summary>
    public string? ThreadId { get; set; }
}
```

**Rationale**: Store the thread ID so we can retrieve the thread on subsequent requests.

---

#### 1.2 Update DbContext Configuration
**Location**: `ObsidianAI.Infrastructure/Data/ObsidianAIDbContext.cs`

**Change**:
```csharp
private static void ConfigureConversation(EntityTypeBuilder<Conversation> builder)
{
    // Existing configuration...
    
    builder.Property(c => c.ThreadId).HasMaxLength(128);
    builder.HasIndex(c => c.ThreadId); // For quick lookup
}
```

---

#### 1.3 Create Migration
```bash
cd ObsidianAI.Infrastructure
dotnet ef migrations add AddThreadIdToConversation --startup-project ../ObsidianAI.Api
```

---

### **Phase 2: Infrastructure Layer - Thread Management** (1 hour)

#### 2.1 Create Thread Provider Interface
**Location**: `ObsidianAI.Domain/Ports/IAgentThreadProvider.cs`

```csharp
using Microsoft.Agents.AI;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Manages AgentThread instances for multi-turn conversations.
/// </summary>
public interface IAgentThreadProvider
{
    /// <summary>
    /// Creates a new AgentThread.
    /// </summary>
    Task<AgentThread> CreateThreadAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves an existing AgentThread by its identifier.
    /// </summary>
    Task<AgentThread?> GetThreadAsync(string threadId, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a thread from storage.
    /// </summary>
    Task DeleteThreadAsync(string threadId, CancellationToken ct = default);
}
```

---

#### 2.2 Implement In-Memory Thread Provider
**Location**: `ObsidianAI.Infrastructure/Agents/InMemoryAgentThreadProvider.cs`

```csharp
using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Infrastructure.Agents;

/// <summary>
/// In-memory implementation of AgentThread provider.
/// Threads are stored in memory and lost on application restart.
/// </summary>
public sealed class InMemoryAgentThreadProvider : IAgentThreadProvider
{
    private readonly ConcurrentDictionary<string, AgentThread> _threads = new();

    public Task<AgentThread> CreateThreadAsync(CancellationToken ct = default)
    {
        var thread = new AgentThread();
        _threads.TryAdd(thread.Id, thread);
        return Task.FromResult(thread);
    }

    public Task<AgentThread?> GetThreadAsync(string threadId, CancellationToken ct = default)
    {
        _threads.TryGetValue(threadId, out var thread);
        return Task.FromResult(thread);
    }

    public Task DeleteThreadAsync(string threadId, CancellationToken ct = default)
    {
        _threads.TryRemove(threadId, out _);
        return Task.CompletedTask;
    }
}
```

**Note**: This is a simple implementation. For production, consider persisting thread state to Redis or SQLite.

---

#### 2.3 Update Agent Implementations to Use Threads
**Location**: `ObsidianAI.Infrastructure/LLM/OpenRouterChatAgent.cs`

**Change**:
```csharp
public sealed class OpenRouterChatAgent : IChatAgent
{
    private readonly ChatClientAgent _agent;
    private readonly IAgentThreadProvider? _threadProvider;

    private OpenRouterChatAgent(
        IOptions<AppSettings> appOptions, 
        string instructions, 
        IEnumerable<object>? tools,
        IAgentThreadProvider? threadProvider = null)
    {
        // Existing initialization...
        _threadProvider = threadProvider;
    }

    public static Task<OpenRouterChatAgent> CreateAsync(
        IOptions<AppSettings> appOptions, 
        string instructions, 
        IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(new OpenRouterChatAgent(appOptions, instructions, tools, threadProvider));
    }

    public async Task<string> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        
        AgentThread? thread = null;
        if (!string.IsNullOrEmpty(threadId) && _threadProvider != null)
        {
            thread = await _threadProvider.GetThreadAsync(threadId, ct);
        }
        
        var response = thread != null
            ? await _agent.RunAsync(thread, message, ct)
            : await _agent.RunAsync(message, ct);
            
        return response?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
        string message, 
        string? threadId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        AgentThread? thread = null;
        if (!string.IsNullOrEmpty(threadId) && _threadProvider != null)
        {
            thread = await _threadProvider.GetThreadAsync(threadId, ct);
        }

        var stream = thread != null
            ? _agent.RunStreamingAsync(thread, message, ct)
            : _agent.RunStreamingAsync(message, ct);
            
        await foreach (var update in stream.WithCancellation(ct).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return ChatStreamEvent.TextChunk(update.Text);
            }

            if (update.Contents is not null)
            {
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent fcc && !string.IsNullOrEmpty(fcc.Name))
                    {
                        yield return ChatStreamEvent.ToolCall(fcc.Name);
                    }
                }
            }
        }
    }
}
```

**Repeat for `LmStudioChatAgent.cs`**

---

#### 2.4 Update IChatAgent Interface
**Location**: `ObsidianAI.Domain/Ports/IChatAgent.cs`

**Change**:
```csharp
public interface IChatAgent
{
    /// <summary>
    /// Sends a single message with optional thread context.
    /// </summary>
    Task<string> SendAsync(string message, string? threadId = null, CancellationToken ct = default);

    /// <summary>
    /// Streams model output with optional thread context.
    /// </summary>
    IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, CancellationToken ct = default);
}
```

---

#### 2.5 Update IAIAgentFactory
**Location**: `ObsidianAI.Domain/Ports/IAIAgentFactory.cs`

**Change**:
```csharp
public interface IAIAgentFactory
{
    // Existing members...
    
    Task<IChatAgent> CreateAgentAsync(
        string instructions, 
        IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default);
}
```

---

#### 2.6 Update ConfiguredAIAgentFactory
**Location**: `ObsidianAI.Infrastructure/LLM/ConfiguredAIAgentFactory.cs`

**Change**:
```csharp
public async Task<IChatAgent> CreateAgentAsync(
    string instructions, 
    IEnumerable<object>? tools = null,
    IAgentThreadProvider? threadProvider = null,
    CancellationToken cancellationToken = default)
{
    return ProviderName.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
        ? await LmStudioChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken)
        : await OpenRouterChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken);
}
```

---

### **Phase 3: Application Layer - Thread Lifecycle** (1 hour)

#### 3.1 Update ConversationPersistenceContext
**Location**: `ObsidianAI.Application/Contracts/ConversationPersistenceContext.cs`

**Change**:
```csharp
public sealed record ConversationPersistenceContext(
    Guid? ConversationId,
    string? UserId,
    ConversationProvider Provider,
    string ModelName,
    string? TitleSource,
    string? ThreadId); // Add ThreadId
```

---

#### 3.2 Update StartChatUseCase
**Location**: `ObsidianAI.Application/UseCases/StartChatUseCase.cs`

**Changes**:
```csharp
public class StartChatUseCase
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly IAgentThreadProvider _threadProvider;
    private readonly IConversationRepository _conversationRepository;
    // ... other dependencies

    public StartChatUseCase(
        IAIAgentFactory agentFactory,
        IAgentThreadProvider threadProvider,
        // ... other dependencies
    )
    {
        _agentFactory = agentFactory;
        _threadProvider = threadProvider;
        // ... other assignments
    }

    public async Task<StartChatResult> ExecuteAsync(
        ChatInput input, 
        string instructions, 
        ConversationPersistenceContext persistenceContext, 
        CancellationToken ct = default)
    {
        // 1. Ensure conversation and thread exist
        var conversation = await EnsureConversationAsync(persistenceContext, input.Message, ct);
        
        // 2. Get or create thread
        string threadId = conversation.ThreadId ?? await CreateAndAssignThreadAsync(conversation.Id, ct);
        
        // 3. Persist user message
        var userMessage = await PersistUserMessageAsync(conversation.Id, input.Message, ct);

        // 4. Discover tools
        IEnumerable<object>? tools = null;
        if (_mcpClientProvider != null)
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync(ct);
            if (mcpClient != null)
            {
                tools = await mcpClient.ListToolsAsync(cancellationToken: ct);
            }
        }

        // 5. Create agent WITH thread provider
        var agent = await _agentFactory.CreateAgentAsync(instructions, tools, _threadProvider, ct);
        
        // 6. Send message WITH thread context
        var responseText = await agent.SendAsync(input.Message, threadId, ct);
        
        // 7. Extract file operation and persist assistant message
        var fileOperation = _extractor.Extract(responseText);
        var assistantMessage = await PersistAssistantMessageAsync(conversation.Id, responseText, fileOperation, ct);

        // 8. Update conversation metadata
        await UpdateConversationMetadataAsync(conversation.Id, persistenceContext.TitleSource ?? input.Message, ct);

        return new StartChatResult(conversation.Id, userMessage.Id, assistantMessage.Id, responseText, fileOperation);
    }

    private async Task<string> CreateAndAssignThreadAsync(Guid conversationId, CancellationToken ct)
    {
        var thread = await _threadProvider.CreateThreadAsync(ct);
        
        // Update conversation with thread ID
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, false, ct);
        if (conversation != null)
        {
            conversation.ThreadId = thread.Id;
            await _conversationRepository.UpdateAsync(conversation, ct);
        }
        
        return thread.Id;
    }

    // Existing helper methods...
}
```

---

#### 3.3 Update StreamChatUseCase
**Location**: `ObsidianAI.Application/UseCases/StreamChatUseCase.cs`

**Similar changes as StartChatUseCase**:
- Inject `IAgentThreadProvider`
- Get/create thread for conversation
- Pass `threadId` to `agent.StreamAsync()`

---

### **Phase 4: API Layer Integration** (30 min)

#### 4.1 Register Thread Provider
**Location**: `ObsidianAI.Infrastructure/DI/ServiceCollectionExtensions.cs`

**Change**:
```csharp
public static IServiceCollection AddObsidianAI(this IServiceCollection services, IConfiguration configuration)
{
    // Existing registrations...
    
    services.AddSingleton<IAgentThreadProvider, InMemoryAgentThreadProvider>();
    
    return services;
}
```

---

#### 4.2 Update Endpoint to Pass ThreadId
**Location**: `ObsidianAI.Api/Configuration/EndpointRegistration.cs`

**Change in BuildPersistenceContext**:
```csharp
private static ConversationPersistenceContext BuildPersistenceContext(
    ChatRequest request, 
    string? userId, 
    AppSettings appSettings, 
    string modelName,
    string? threadId = null) // Add parameter
{
    var provider = ParseProvider(appSettings.LLM.Provider);
    return new ConversationPersistenceContext(
        request.ConversationId, 
        userId, 
        provider, 
        modelName, 
        request.Message,
        threadId); // Pass threadId
}
```

**Update /chat endpoint**:
```csharp
app.MapPost("/chat", async (
    ChatRequest request,
    StartChatUseCase useCase,
    ILlmClientFactory llmClientFactory,
    IConversationRepository conversationRepository,
    IOptions<AppSettings> appSettings,
    CancellationToken cancellationToken) =>
{
    var instructions = AgentInstructions.ObsidianAssistant;

    // Load conversation to get threadId
    string? threadId = null;
    if (request.ConversationId.HasValue)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId.Value, false, cancellationToken);
        threadId = conversation?.ThreadId;
    }

    var input = new ChatInput(request.Message, history: null); // History now managed by thread
    var context = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName(), threadId);
    var result = await useCase.ExecuteAsync(input, instructions, context, cancellationToken);

    return Results.Ok(new
    {
        conversationId = result.ConversationId,
        userMessageId = result.UserMessageId,
        assistantMessageId = result.AssistantMessageId,
        text = result.Text,
        fileOperationResult = result.FileOperation == null ? null : new FileOperationData(result.FileOperation.Action, result.FileOperation.FilePath)
    });
});
```

**Update /chat/stream endpoint similarly**

---

### **Phase 5: Remove Manual History Management** (30 min)

#### 5.1 Simplify ChatInput
**Location**: `ObsidianAI.Domain/Models/ChatInput.cs`

**Change**:
```csharp
public sealed record ChatInput
{
    public ChatInput()
    {
        Message = string.Empty;
    }

    public ChatInput(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }
        Message = message;
    }

    public string Message { get; init; }
    
    // REMOVE: History property - no longer needed!
}
```

---

#### 5.2 Update API Models
**Location**: `ObsidianAI.Api/Models/Records.cs`

**Change**:
```csharp
public record ChatRequest(string Message, Guid? ConversationId = null);
// REMOVE: List<ChatMessage>? History parameter
```

---

#### 5.3 Clean Up Web Layer
**Location**: `ObsidianAI.Web/Hubs/ChatHub.cs`

**Remove history parameter from StreamMessage**:
```csharp
public async Task StreamMessage(string message, string? conversationId)
{
    // Remove history parameter and references
    var requestBody = new
    {
        message,
        conversationId
        // REMOVE: history
    };
    // ... rest of implementation
}
```

**Location**: `ObsidianAI.Web/Components/Pages/Chat.razor`

**Remove history tracking from SendMessage**:
```csharp
private async Task SendMessage()
{
    // ... existing code
    
    await hubConnection.SendAsync("StreamMessage", messageToSend, currentConversationId?.ToString());
    // REMOVE: conversationHistory parameter
}
```

---

### **Phase 6: Testing & Verification** (1 hour)

#### Test Scenarios

**Test 1: New Conversation with Multi-Turn**
```
User: "What files are in my vault?"
Agent: [Lists files]
User: "Tell me more about the first one" ← Should remember context
Agent: [Discusses first file from previous list]
```

**Test 2: Resume Conversation After Reload**
```
1. Start conversation, send 2 messages
2. Note conversation ID
3. Refresh page
4. Load conversation via sidebar
5. Send follow-up message
6. Verify agent remembers previous messages
```

**Test 3: Multiple Conversations**
```
1. Create Conversation A, discuss topic X
2. Create Conversation B, discuss topic Y
3. Switch back to Conversation A
4. Verify context is still about topic X
```

**Test 4: Thread Cleanup**
```
1. Delete conversation
2. Verify thread is removed from provider
3. Attempt to use deleted thread ID (should fail gracefully)
```

---

## Migration Strategy

### Option A: Fresh Start (Recommended)
- Run new migration
- Existing conversations will have `ThreadId = null`
- On first message in old conversation, create thread and backfill
- No data loss

### Option B: Backfill Threads
Create a one-time script to:
```csharp
var conversations = await _repository.GetAllAsync(...);
foreach (var conversation in conversations.Where(c => c.ThreadId == null))
{
    var thread = await _threadProvider.CreateThreadAsync();
    conversation.ThreadId = thread.Id;
    await _repository.UpdateAsync(conversation);
}
```

---

## Benefits

1. **Automatic Context** - No manual history tracking
2. **Cleaner Code** - Remove history passing logic
3. **Better UX** - Agent remembers entire conversation
4. **Tool Call History** - Framework tracks tool invocations
5. **Scalable** - Thread state can be persisted externally

---

## Timeline

| Phase | Duration | Priority |
|-------|----------|----------|
| Phase 1: Domain | 30 min | Critical |
| Phase 2: Infrastructure | 1 hour | Critical |
| Phase 3: Application | 1 hour | Critical |
| Phase 4: API | 30 min | Critical |
| Phase 5: Cleanup | 30 min | High |
| Phase 6: Testing | 1 hour | High |

**Total Estimated Time: 4.5 hours**

---

## Next Steps

1. ✅ Review this plan
2. Create feature branch: `feature/agent-thread-integration`
3. Start with Phase 1 (add migration)
4. Implement phases sequentially
5. Test after each phase
6. Deploy to staging before production

Ready to implement Phase 1?