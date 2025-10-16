# application-contracts Style Guide

- Define contracts as `record` types (sealed where appropriate) to capture use-case IO without leaking infrastructure details.
- Place conversation-related DTOs in this folder so use cases can share them across API and UI adapters.
- Reference domain enums (e.g., `ConversationProvider`) rather than duplicating enum definitions.
- Keep constructor parameter order stable—the API relies on positional record constructors for serialization.
