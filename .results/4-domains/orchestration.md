# Orchestration Domain Deep Dive

ObsidianAI leverages .NET Aspire to compose the API, web frontend, and MCP gateway. Shared service defaults provide observability, health checks, and resilient HTTP client configuration.

## Key Conventions
- **DistributedApplication builder:** `ObsidianAI.AppHost/AppHost.cs` defines executable and project resources, wiring dependencies with `WithReference` and environment variables.
- **Service defaults:** Each project calls `builder.AddServiceDefaults()` (from `ObsidianAI.ServiceDefaults/Extensions.cs`) to opt into shared OpenTelemetry, health checks, and service discovery.
- **MCP endpoint contract:** The API receives `MCP_ENDPOINT=http://localhost:8033/mcp`, aligning with the gateway spawned by the host.
- **Resilient HttpClient:** `ConfigureHttpClientDefaults` enables retry policies and service discovery for all named HttpClients, including the SignalR hub’s use of `IHttpClientFactory`.

## Representative Code
### Aspire resource declaration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", workingDirectory: ".", args: [
    "mcp", "gateway", "run",
    "--transport", "streaming",
    "--port", "8033",
    "--servers", "obsidian"
]);

var api = builder.AddProject<Projects.ObsidianAI_Api>("api")
    .WithEnvironment("MCP_ENDPOINT", "http://localhost:8033/mcp")
    .WaitFor(mcpGateway);

var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### Shared service defaults
```csharp
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
{
    builder.ConfigureOpenTelemetry();

    builder.AddDefaultHealthChecks();

    builder.Services.AddServiceDiscovery();

    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

    return builder;
}
```

## Implementation Notes
- **Startup ordering:** `api.WaitFor(mcpGateway)` ensures the API waits until the MCP process is ready, preventing tool discovery errors at boot.
- **Telemetry exporters:** `AddServiceDefaults` wires OpenTelemetry logging/metrics/tracing and optionally uses OTLP exporters when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- **Health endpoints:** `MapDefaultEndpoints()` exposes `/health` and `/alive` in development only, aligning with Aspire guidance.
- **Environment-driven config:** Other services that need the MCP gateway should reuse the same `MCP_ENDPOINT` environment key to remain portable across host environments.
- **Service discovery:** HttpClients created with `builder.Services.AddHttpClient("ObsidianAI.Api", …)` automatically participate in service discovery, enabling Aspire to resolve inter-service URLs without hard-coded ports in production.
