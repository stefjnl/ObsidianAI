# API Domain Deep Dive

The API layer is built with ASP.NET Core minimal APIs and centralizes endpoint registration in `ObsidianAI.Api/Configuration/EndpointRegistration.cs`. Every route composes Application-layer use cases, resolves infrastructure services through dependency injection, and avoids direct persistence access.

## Key Conventions
- **Single registration surface:** `Program.cs` calls `MapObsidianEndpoints()` after applying shared defaults, so all new routes should extend the existing extension method.
- **Use-case orchestration:** Endpoint lambdas receive concrete use case classes (e.g., `ListConversationsUseCase`, `StartChatUseCase`) and translate their results into API DTOs or anonymous payloads.
- **Provider awareness:** LLM provider/model metadata is resolved from `ILlmClientFactory` and `IOptions<AppSettings>` inside endpoints, ensuring consistent logging and response shaping.
- **Streaming abstraction:** The `/chat/stream` route hands off payload streaming to `StreamingEventWriter.WriteAsync`, which formats SSE events that the Blazor client consumes through SignalR.

## Representative Code
### Endpoint wiring in `Program.cs`
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddObsidianApiServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapObsidianEndpoints();
app.MapHealthChecks("/healthz");
```

### Minimal API definition pattern
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

    string? threadId = null;
    if (request.ConversationId.HasValue)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId.Value, includeMessages: false, cancellationToken).ConfigureAwait(false);
        threadId = conversation?.ThreadId;
    }

    var input = new ChatInput(request.Message);
    var context = BuildPersistenceContext(request, null, appSettings.Value, llmClientFactory.GetModelName(), threadId);
    var result = await useCase.ExecuteAsync(input, instructions, context, cancellationToken).ConfigureAwait(false);

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

### Streaming helper contract
```csharp
public static async Task WriteAsync(HttpContext context, IAsyncEnumerable<ChatStreamEvent> events, ILogger logger, CancellationToken ct = default)
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    var updateCount = 0;

    try
    {
        await foreach (var update in events)
        {
            updateCount++;

            if (update.Kind == ChatStreamEventKind.ToolCall)
            {
                logger.LogInformation("Sending tool_call event: {ToolName}", update.ToolName);
                await context.Response.WriteAsync($"event: tool_call\ndata: {update.ToolName}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
            else if (update.Kind == ChatStreamEventKind.Metadata && !string.IsNullOrEmpty(update.Metadata))
            {
                logger.LogInformation("Sending metadata event: {Payload}", update.Metadata);
                await context.Response.WriteAsync($"event: metadata\ndata: {update.Metadata}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
            else if (update.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(update.Text))
            {
                var escapedContent = update.Text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                logger.LogInformation("Sending token #{Count}: RAW='{Escaped}' (Length={Length})",
                    updateCount,
                    escapedContent.Length > 100 ? escapedContent.Substring(0, 100) + "..." : escapedContent,
                    update.Text.Length);

                await context.Response.WriteAsync($"data: {update.Text}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }

        logger.LogInformation("Stream complete. Sending [DONE] marker after {Count} updates", updateCount);
        await context.Response.WriteAsync("data: [DONE]\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during streaming after {Count} updates", updateCount);
        await context.Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
}
```

## Implementation Notes
- Health checks (`/healthz`) remain separate from the main endpoint extension to keep readiness logic isolated.
- Endpoint lambdas frequently project `DateTime` and enum values into primitives to keep serialized payloads lean and UI-friendly.
- Streaming responses rely on the exact SSE contract (`tool_call`, `metadata`, `[DONE]` markers); maintaining this contract is critical for the SignalR bridge on the frontend.
