# web-services Style Guide

- Services bridge the UI and API; expose async methods that return UI-friendly models and handle mapping internally.
- Inject `HttpClient` or SignalR clients via DI; avoid constructing them manually to keep testing simple.
- Normalize streaming payloads into the `ChatMessage`/`ConversationDetail` models so components stay presentation-focused.
