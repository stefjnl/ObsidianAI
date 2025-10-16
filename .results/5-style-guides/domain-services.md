# domain-services Style Guide

- Keep interfaces focused on pure domain logic; avoid infrastructure concerns and stick to vocabulary from `ObsidianAI.Domain` entities.
- Prefer stateless abstractions when possible; if state is required, make lifetime expectations explicit through XML doc comments.
- Ensure all methods are asynchronous or accept `CancellationToken` when they will be implemented with I/O in lower layers.
- Place cross-cutting behavior (e.g., file normalization rules) here so it can be reused by the application layer without violating directionality.
