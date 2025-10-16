# api-streaming Style Guide

- Use `StreamingEventWriter.WriteAsync` for all SSE responses; it standardizes headers, flush cadence, and event naming conventions (`tool_call`, `metadata`, standard `data:` lines).
- Flush the HTTP response after every event to keep SignalR clients responsive; the helper already handles this—do not build ad-hoc SSE writers elsewhere.
- Log each update with escaped control characters so diagnostics reveal the raw token stream; copy the existing logging format if you add new event types.
- Always send a `data: [DONE]` marker when the stream completes to signal downstream consumers to finalize UI state.
