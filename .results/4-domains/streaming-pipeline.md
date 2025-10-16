# Streaming Pipeline Deep Dive

Streaming spans the API, SignalR hub, and Blazor page to deliver token-by-token assistant output and metadata updates.

## Key Stages
1. **SSE emission:** `StreamingEventWriter.WriteAsync` serializes `ChatStreamEvent` instances as `data:` records and emits custom `event:` markers (`tool_call`, `metadata`, `error`).
2. **SignalR bridge:** `ChatHub.StreamMessage` forwards SSE output to the caller, batching raw token chunks and publishing hub events (`ReceiveToken`, `Metadata`, `StatusUpdate`, `MessageComplete`).
3. **UI promotion:** `Chat.razor` consumes hub callbacks, promotes optimistic messages to persisted IDs, and attaches action cards or file operations based on streaming metadata.

## Representative Code
### Bridging SSE to SignalR (`ChatHub`)
```csharp
while ((line = await reader.ReadLineAsync()) != null)
{
    lineCount++;

    // Log with escaped characters to see actual structure
    var escapedLine = line.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    _logger.LogInformation("Line #{LineNum}: '{EscapedLine}'", lineCount,
        escapedLine.Length > 100 ? escapedLine.Substring(0, 100) + "..." : escapedLine);

    if (line.StartsWith("event: "))
    {
        currentEvent = line.Substring(7).Trim();
    }
    else if (line.StartsWith("data: "))
    {
        var data = line.Substring(6);

        if (data == "[DONE]")
        {
            await FlushTokenBufferAsync(force: true);
            var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());
            await Clients.Caller.SendAsync("MessageComplete", finalResponse);
            completionSent = true;
            break;
        }

        if (currentEvent == "tool_call")
        {
            await Clients.Caller.SendAsync("StatusUpdate", new { type = "tool_call", tool = data });
            currentEvent = null;
        }
        else if (currentEvent == "metadata")
        {
            await Clients.Caller.SendAsync("Metadata", data);
            currentEvent = null;
        }
        else if (currentEvent == "error")
        {
            await FlushTokenBufferAsync(force: true);
            await Clients.Caller.SendAsync("Error", data);
            currentEvent = null;
            completionSent = true;
            break;
        }
        else
        {
            var decodedChunk = TextDecoderService.DecodeSurrogatePairs(data);
            fullResponse.Append(decodedChunk);
            tokenBuffer.Append(decodedChunk);
            await FlushTokenBufferAsync();
        }
    }
    else if (string.IsNullOrWhiteSpace(line))
    {
        currentEvent = null;
    }
}
```

### Promoting optimistic messages (`Chat.razor`)
```csharp
private async Task HandleMetadataAsync(string metadataJson)
{
    StreamingMetadata? metadata = null;
    try
    {
        metadata = JsonSerializer.Deserialize<StreamingMetadata>(metadataJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (JsonException ex)
    {
        Logger.LogError(ex, "Invalid metadata payload: {Metadata}", metadataJson);
        return;
    }

    if (metadata == null)
    {
        return;
    }

    await InvokeAsync(() =>
    {
        var conversationId = metadata.ConversationId;
        if (!currentConversationId.HasValue || currentConversationId != conversationId)
        {
            currentConversationId = conversationId;
            UpdateConversationRoute(conversationId);
        }

        PromoteUserMessage(metadata);
        PromoteAssistantMessage(metadata);

        if (currentConversationMetadata != null)
        {
            currentConversationMetadata = currentConversationMetadata with
            {
                UpdatedAt = DateTime.UtcNow,
                MessageCount = conversationHistory.Count
            };
        }

        UpdateCurrentConversationSummarySnapshot();
        _ = RefreshConversationListAsync();
        StateHasChanged();
        return Task.CompletedTask;
    });
}
```

## Implementation Notes
- **Batching threshold:** Token flushing defaults to 50 characters to balance responsiveness and DOM churn; altering this threshold affects perceived latency.
- **Event contract:** `tool_call`, `metadata`, `error`, and `[DONE]` markers originate from the API; both hub and UI must handle them to keep messages in sync.
- **Unicode safety:** `TextDecoderService.DecodeSurrogatePairs` ensures partial UTF-16 fragments stream without rendering errors.
- **Cancellation propagation:** `ChatHub.StreamMessage` passes `Context.ConnectionAborted` to the downstream HTTP request, cancelling SSE reads when the Blazor circuit shuts down.
- **Optimistic IDs:** The UI stores temporary message IDs (`pendingUserMessageClientId`, `pendingAssistantMessageClientId`) and replaces them once metadata arrives, preventing duplicate UI entries.
