# infrastructure-healthchecks Style Guide

- Implement `IHealthCheck` adapters that exercise real dependencies (ping LLM endpoints, MCP gateway) to catch runtime failures early.
- Keep health check names consistent with API registrations so dashboards map correctly; update both when renaming.
- Use timeouts and cancellation tokens to prevent slow external calls from blocking the health probe pipeline.
