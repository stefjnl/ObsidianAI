# api-service-implementations Style Guide

- Service factories (`LmStudioClientFactory`, `OpenRouterClientFactory`) wrap provider SDK clients and immediately call `.AsIChatClient()` so the Microsoft Agent Framework abstractions stay consistent—reuse this pattern for new providers.
- `ObsidianAssistantService` guards agent initialization with a `SemaphoreSlim` and `_isInitialized` flag; any new initialization logic should respect the same double-check locking structure to stay thread-safe.
- When MCP tooling is unavailable, the assistant logs a warning and proceeds without tools; follow this graceful-degradation pattern in future services to keep the assistant responsive.
- LLM services cache the created `IChatClient` for reuse; avoid per-request instantiation to prevent unnecessary connection churn.
