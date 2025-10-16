# Application Layer Deep Dive

The Application project orchestrates domain logic through focused use case classes. Each use case coordinates domain ports, infrastructure services, and cross-cutting helpers while remaining persistence-agnostic.

## Key Conventions
- **Use case per workflow:** Classes in `ObsidianAI.Application/UseCases` encapsulate end-to-end scenarios (`StartChatUseCase`, `ListVaultContentsUseCase`, etc.), exposing async `ExecuteAsync` methods.
- **Port-driven dependencies:** Constructor injection relies on domain interfaces (`IAIAgentFactory`, `IAgentThreadProvider`, `IConversationRepository`) instead of infrastructure types, preserving Clean Architecture boundaries.
- **Optional infrastructure hooks:** Where available, use cases resolve optional services (`IMcpClientProvider`) and degrade gracefully when unavailable.
- **Result contracts:** Use cases return DTOs declared under `ObsidianAI.Application/Contracts` to keep serialization-friendly payloads separate from entities.

## Representative Code
### Orchestrating a chat round trip (`StartChatUseCase`)
```csharp
public async Task<Contracts.StartChatResult> ExecuteAsync(ChatInput input, string instructions, ConversationPersistenceContext persistenceContext, CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(persistenceContext);

    if (string.IsNullOrWhiteSpace(input.Message))
    {
        throw new ArgumentException("Message cannot be null or whitespace.", nameof(input));
    }

    var conversation = await EnsureConversationAsync(persistenceContext, input.Message, ct).ConfigureAwait(false);

    IEnumerable<object>? tools = null;
    if (_mcpClientProvider != null)
    {
        var mcpClient = await _mcpClientProvider.GetClientAsync(ct).ConfigureAwait(false);
        if (mcpClient != null)
        {
            tools = await mcpClient.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);
        }
    }

    var agent = await _agentFactory.CreateAgentAsync(instructions, tools, _threadProvider, ct).ConfigureAwait(false);
    var threadId = await EnsureThreadAsync(conversation, persistenceContext.ThreadId, agent, ct).ConfigureAwait(false);
    var userMessage = await PersistUserMessageAsync(conversation.Id, input.Message, ct).ConfigureAwait(false);
    var responseText = await agent.SendAsync(input.Message, threadId, ct).ConfigureAwait(false);
    var fileOperation = _extractor.Extract(responseText);
    var resolvedOperation = await ResolveFileOperationAsync(fileOperation, ct).ConfigureAwait(false);

    var assistantMessage = await PersistAssistantMessageAsync(conversation.Id, responseText, resolvedOperation, ct).ConfigureAwait(false);

    await UpdateConversationMetadataAsync(conversation.Id, persistenceContext.TitleSource ?? input.Message, ct).ConfigureAwait(false);

    return new Contracts.StartChatResult(conversation.Id, userMessage.Id, assistantMessage.Id, responseText, resolvedOperation);
}
```

### Thread management pattern
```csharp
private async Task<string> EnsureThreadAsync(Conversation conversation, string? persistedThreadId, IChatAgent agent, CancellationToken ct)
{
    if (conversation == null)
    {
        throw new ArgumentNullException(nameof(conversation));
    }

    var candidateId = !string.IsNullOrEmpty(conversation.ThreadId)
        ? conversation.ThreadId
        : persistedThreadId;

    if (!string.IsNullOrEmpty(candidateId))
    {
        var existing = await _threadProvider.GetThreadAsync(candidateId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.Equals(conversation.ThreadId, candidateId, StringComparison.Ordinal))
            {
                conversation.ThreadId = candidateId;
                await _conversationRepository.UpdateAsync(conversation, ct).ConfigureAwait(false);
            }

            return candidateId;
        }
    }

    var thread = await agent.CreateThreadAsync(ct).ConfigureAwait(false);
    var threadId = await _threadProvider.RegisterThreadAsync(thread, ct).ConfigureAwait(false);
    conversation.ThreadId = threadId;
    await _conversationRepository.UpdateAsync(conversation, ct).ConfigureAwait(false);
    return threadId;
}
```

## Implementation Notes
- **Fallback strategies:** Optional collaborators (`IMcpClientProvider`) are nullable and always checked before use, ensuring features continue when the MCP gateway is down.
- **Consistency helpers:** `VaultPathResolver` is invoked before persisting file operations, guaranteeing user input aligns with vault casing/path conventions.
- **Telemetry hooks:** Use cases accept `ILogger<T>` instances for detailed tracing and error reporting without leaking infrastructure concerns.
- **Title generation:** `StartChatUseCase` derives conversation titles from the first user prompt via `CreateTitle`, mirroring the UI’s optimistic behavior.
