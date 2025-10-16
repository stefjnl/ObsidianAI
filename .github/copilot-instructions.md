# ObsidianAI Copilot Instructions

## Overview
- Clean Architecture solution spanning Domain, Application, Infrastructure, API, Web, and Aspire orchestration projects with strict dependency flow (outer layers depend inward only).
- Core capabilities: Obsidian vault management via MCP tools, multi-provider LLM access (LM Studio, OpenRouter) through Microsoft Agent Framework, and Blazor Server UI with SignalR streaming.
- Data persistence handled by EF Core + SQLite with repositories, action card auditing, and full conversation history, surfaced through minimal APIs and real-time web components.
- Observability and runtime cohesion delivered by .NET Aspire, shared ServiceDefaults, health checks, and configuration POCOs that align environment settings across services.

## Category Reference
Detailed conventions for every repository bucket are recorded in `.results/5-style-guides/<category>.md`. Use the table below to locate them quickly.

| Area | Categories | Style Guide Paths |
| --- | --- | --- |
| Solution & Docs | `solution-config`, `root-environment`, `project-documentation`, `analysis-output`, `visualstudio-cache` | `.results/5-style-guides/solution-config.md`, `root-environment.md`, `project-documentation.md`, `analysis-output.md`, `visualstudio-cache.md` |
| API Layer | `api-config-json`, `api-docker`, `api-project`, `api-http-samples`, `api-entrypoint`, `api-configuration`, `api-health-checks`, `api-helpers`, `api-models`, `api-service-contracts`, `api-service-implementations`, `api-streaming`, `api-launchsettings` | `.results/5-style-guides/api-*.md` (one per category) |
| Aspire & Defaults | `aspire-orchestration`, `aspire-config`, `aspire-project`, `aspire-launchsettings`, `service-defaults-extensions`, `service-defaults-project` | `.results/5-style-guides/aspire-*.md`, `service-defaults-*.md` |
| Application & Domain | `application-assembly-info`, `application-project`, `application-contracts`, `application-di`, `application-dtos`, `application-mappers`, `application-services`, `application-usecases`, `domain-assembly-info`, `domain-project`, `domain-entities`, `domain-models`, `domain-ports`, `domain-services` | `.results/5-style-guides/application-*.md`, `domain-*.md` |
| Infrastructure | `infrastructure-assembly-info`, `infrastructure-project`, `infrastructure-agents`, `infrastructure-configuration`, `infrastructure-data-context`, `infrastructure-migrations`, `infrastructure-repositories`, `infrastructure-di`, `infrastructure-healthchecks`, `infrastructure-llm`, `infrastructure-vault` | `.results/5-style-guides/infrastructure-*.md` |
| Web Layer | `web-appsettings`, `web-dockerfile`, `web-project`, `web-program`, `web-hubs`, `web-services`, `web-models`, `web-components-root`, `web-components-routes`, `web-components-layout`, `web-components-pages`, `web-components-shared`, `web-properties`, `web-styles`, `web-static-assets`, `web-scripts`, `web-vendor-bootstrap-css`, `web-vendor-bootstrap-js` | `.results/5-style-guides/web-*.md` |
| Testing & Data | `tests-project`, `tests-application`, `tests-infrastructure`, `tests-support`, `sample-databases` | `.results/5-style-guides/tests-*.md`, `sample-databases.md` |

## Feature Scaffold Guide
1. Shape domain changes first: extend entities/value objects in `ObsidianAI.Domain`, add or refine ports for new integrations, and document invariants in the matching domain style guides.
2. Add application behavior: introduce DTOs/mappers as needed, implement or update use cases, and register services in `Application/DI` while keeping async flows and cancellation ubiquitous.
3. Bridge infrastructure: implement new ports (repositories, MCP adapters, agents) inside Infrastructure, add configuration binders, migrations, and wire everything through `ServiceCollectionExtensions`.
4. Expose through API & Web: create or adjust minimal API endpoints and streaming writers, then surface features via Blazor components, SignalR hubs, and web services with matching models.
5. Verify and document: extend test suites under `ObsidianAI.Tests`, refresh relevant style guides or docs, and confirm Aspire orchestration plus Docker workflows still build and run.

## Integration Rules
- Maintain dependency direction: Web → API → Application → Domain, while Infrastructure implements Domain ports and is only invoked via DI-registered abstractions.
- Treat configuration as contracts: update strongly-typed settings alongside `appsettings.*` files and surface validation inside DI extensions to fail fast on misconfiguration.
- Embrace asynchronous streaming: APIs should buffer SSE events responsibly, and web components must handle incremental SignalR payloads with graceful cancellation.
- Keep tool execution auditable: any MCP or vault operation should produce `FileOperationRecord` entries and respect the confirmation flow enforced by action cards.
- Align observability: whenever you add services or hosts, extend ServiceDefaults and Aspire manifests so logging, health checks, and environment variables flow uniformly.

## Example Prompt Usage
- "Pull the latest chat streaming pipeline changes and highlight any adjustments needed in `api-streaming` and corresponding style guides."
- "Draft a new use case for exporting conversations to Markdown and outline the Domain → Application → Infrastructure touches."
- "Summarize the DI registrations that relate to MCP tool execution and verify they meet our integration rules."
- "Propose UI updates for the action card confirmation flow and call out which Blazor components, services, and models require edits."

## Verification
- `dotnet test` to validate application and infrastructure changes after new features or refactors.
- `dotnet run --project ObsidianAI.AppHost` to exercise Aspire orchestration and ensure API/Web/MCP services boot together.
- `docker compose up --build` when validating containerized deployments or sharing reproducible demos.
- `curl.exe -X GET http://localhost:5095/health` (or Aspire dashboard) to confirm health checks remain green after infrastructure adjustments.
