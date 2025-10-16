# infrastructure-repositories Style Guide

- Repositories provide domain-facing persistence; keep method signatures aligned with `IConversationRepository` and `IMessageRepository` contracts.
- Wrap EF Core operations in async calls and include `CancellationToken` support to honor upstream cancellation.
- Ensure eager loading includes are explicit to avoid lazy-loading surprises; document query expectations in XML summaries when complex.
