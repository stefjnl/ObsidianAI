# infrastructure-di Style Guide

- Centralize container registrations in `ServiceCollectionExtensions`; expose extension methods grouped by feature (`AddPersistence`, `AddLlmAgents`, etc.).
- Fail fast when required configuration is missing by throwing `InvalidOperationException` during registration rather than at first use.
- Keep DI extensions free of application or web project references to preserve dependency flow.
