# application-mappers Style Guide

- Keep mapping logic in static extension classes (`ConversationMapper`) so call sites read naturally (`entity.ToDto()`).
- Defensive programming: call `ArgumentNullException.ThrowIfNull` before mapping to surface issues early.
- Maintain deterministic ordering (e.g., messages ordered by timestamp, planned actions by `SortOrder`) so downstream UI renders predictable histories.
- Convert enums to strings when projecting to DTOs, matching current API responses and avoiding client-side enum definitions.
