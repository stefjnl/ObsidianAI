# infrastructure-llm Style Guide

- Encapsulate provider-specific logic (`LmStudio`, `OpenRouter`) behind domain ports; constructors should accept typed settings objects rather than raw strings.
- When streaming responses, prefer `IAsyncEnumerable`/channel patterns and surface structured events defined in `ChatStreamEvent`.
- Ensure all HTTP calls use resilient policies (retry, timeout) defined centrally so providers share consistent behavior.
