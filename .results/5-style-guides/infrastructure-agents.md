# infrastructure-agents Style Guide

- Implement domain `IAgentThreadProvider` adapters here; keep constructor signatures small and rely on DI to supply settings from configuration.
- Prefer in-memory or stub implementations for local development; document any external dependencies in XML docs for replacement providers.
- Ensure thread-safe operations when interacting with concurrent chat streams; use `ConcurrentDictionary` or locks if state must be shared.
