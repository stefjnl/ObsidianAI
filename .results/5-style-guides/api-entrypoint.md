# api-entrypoint Style Guide

- Always call `builder.AddServiceDefaults()` before registering API services; this imports shared logging, health checks, and HTTP client policies.
- Delegate container wiring to `AddObsidianApiServices(builder.Configuration)` so Startup logic stays in extension classes rather than `Program.cs`.
- After building the app, map endpoints in this order: `MapDefaultEndpoints()`, `MapObsidianEndpoints()`, and finally health checks on `/healthz` to align with current diagnostics expectations.
- Run EF Core migrations at startup by creating a scoped `ObsidianAIDbContext` and invoking `Database.MigrateAsync()`—maintain this pattern whenever new contexts are added.
- Log the active LLM provider and model at boot using `ILlmClientFactory.GetModelName()` so container logs capture environment configuration.
