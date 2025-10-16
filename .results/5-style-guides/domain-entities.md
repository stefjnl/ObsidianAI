# domain-entities Style Guide

- Entities (`Conversation`, `Message`, `ActionCardRecord`, etc.) live under `ObsidianAI.Domain/Entities` and use C# classes with XML documentation; continue annotating properties to clarify domain intent.
- Use GUID primary keys with mutable properties; persistence configuration sets `ValueGeneratedNever`, so constructors should not assume EF will assign IDs.
- Provide helper methods for aggregate behavior (`Conversation.Touch()`, `Conversation.AddMessage`) instead of letting higher layers manipulate collections directly.
- Include concurrency tokens (`byte[]? RowVersion`) on aggregates that require optimistic concurrency checks.
