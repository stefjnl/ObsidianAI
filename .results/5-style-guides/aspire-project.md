# aspire-project Style Guide

- Keep the AppHost project using `Aspire.AppHost.Sdk` with `net9.0`; Aspire tooling expects this exact SDK pairing.
- Reference only the service projects (`ObsidianAI.Api`, `ObsidianAI.Web`); avoid pulling in Application/Infrastructure here to maintain orchestration isolation.
- Match package versions across `Aspire.Hosting.*` dependencies to avoid build-time incompatibilities.
- Store secrets via `UserSecretsId` when local orchestration needs credentials; do not embed them in source.
