# service-defaults-extensions Style Guide

- Treat `Extensions.cs` as a shared configuration hub (logging, resilience, telemetry); keep methods composable (`AddDefaultResilience`, `AddSerilog`).
- Avoid referencing application-specific services; only register cross-cutting concerns that multiple projects consume.
- Update Aspire orchestration when new defaults are added so they propagate to every hosted service.
