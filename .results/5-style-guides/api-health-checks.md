# api-health-checks Style Guide

- Health checks should validate application dependencies by exercising the same abstractions the runtime uses; `LlmHealthCheck` calls `ILlmClientFactory.CreateChatClient()` instead of pinging providers directly.
- Return meaningful status messages (e.g., include the resolved model name) to simplify diagnosing misconfiguration from the Aspire dashboard.
- Catch all exceptions and translate them into `HealthCheckResult.Unhealthy`; do not let factory exceptions bubble out and crash the health probe pipeline.
