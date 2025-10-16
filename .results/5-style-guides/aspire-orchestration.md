# aspire-orchestration Style Guide

- Define Aspire resources in `AppHost.cs` using the fluent builder; keep executable tools (MCP gateway) as `AddExecutable` resources with explicit command arguments.
- Chain `.WithReference()` to express dependencies between projects; this ensures service discovery wiring is automatic.
- Use `.WaitFor(...)` when the API depends on supporting processes (like MCP) so orchestration waits for readiness before starting downstream services.
- Store cross-service endpoints in environment variables (`MCP_ENDPOINT`) rather than hardcoding them inside consuming projects.
