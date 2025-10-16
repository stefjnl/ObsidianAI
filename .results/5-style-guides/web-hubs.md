# web-hubs Style Guide

- Keep `ChatHub` as the single SignalR hub; expose strongly typed methods that map directly to application use cases.
- Secure hub methods via authorization attributes or connection validation; log connection lifecycle events for diagnostics.
- Use `CancellationToken` on streaming invocations to respect client disconnects.
