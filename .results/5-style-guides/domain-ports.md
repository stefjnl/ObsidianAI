# domain-ports Style Guide

- Define integration boundaries via interfaces in `ObsidianAI.Domain/Ports`; they should be technology agnostic and describe behavior using domain terminology (e.g., `IAIAgentFactory`, `IVaultToolExecutor`).
- All persistence ports return `Task`/`Task<T>` and accept `CancellationToken` parameters to integrate smoothly with async pipelines.
- Keep method names action-oriented (`CreateAsync`, `ArchiveAsync`, `RegisterThreadAsync`) to signal side effects clearly.
- Add new ports in this folder before implementing them in infrastructure to maintain dependency direction.
