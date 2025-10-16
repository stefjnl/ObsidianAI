# application-usecases Style Guide

- Each use case class encapsulates a single workflow and exposes an async `ExecuteAsync` (or similarly named) method; match that signature when introducing new scenarios.
- Inject only the domain ports required for the workflow—avoid reaching into infrastructure classes directly.
- Use the conversation repositories to manage persistence; call `Touch()`/`UpdateAsync()` on aggregates rather than mutating EF contexts inline.
- Where user confirmation or threaded context is needed (e.g., `StartChatUseCase`, `StreamChatUseCase`), centralize that logic inside the use case so API endpoints remain thin.
- Preserve cancellation token propagation by threading `CancellationToken` parameters through each repository and service call.
