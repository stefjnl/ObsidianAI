# application-dtos Style Guide

- Represent DTOs as sealed records to keep mapping lightweight and immutable.
- Keep property sets aligned with UI and API needs (e.g., `ConversationDto` includes provider/model strings for display without exposing enums).
- Document intent with XML doc comments; each DTO explains where it is used (list views, detail views, etc.).
