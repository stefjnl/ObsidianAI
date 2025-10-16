# api-models Style Guide

- Define API payloads as C# `record` types in `Models/Records.cs`; this keeps request/response contracts succinct and immutable.
- Stick with PascalCase property names—they serialize to camelCase by default via System.Text.Json but remain readable in code.
- Include nullable types where fields are optional to avoid runtime binding issues (e.g., `Guid? ConversationId`, optional timestamps).
- Group related payloads together (chat, search, vault operations) so consumers can discover them in a single file.
