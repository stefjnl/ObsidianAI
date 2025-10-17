
## Implementation Plan: Microsoft Learn MCP Direct Connection (Detailed)

This document expands the minimal plan into a concrete, actionable implementation guide for adding Microsoft Learn MCP tools to ObsidianAI. It covers design, DI wiring, configuration, code samples, tests, health checks, rollout, timeline, risk mitigation, and verification steps.

High-level goal
- Add a resilient, testable client for Microsoft Learn's MCP endpoint and expose Microsoft Learn tools to the existing agent toolchain so agents can query Microsoft docs, fetch full articles, and find code samples.

Scope and success criteria
- New interface `IMicrosoftLearnMcpClient` and implementation in `ObsidianAI.Infrastructure`.
- Agent merge: `ObsidianAI.Api` will load Microsoft Learn tools and merge them with Obsidian tools.
- Health check and basic metrics for MCP connectivity.
- Unit / integration tests covering client behavior and agent merging.
- No breaking changes to existing APIs.

Assumptions
- The project uses Microsoft Agent Framework or a similar MCP client pattern and already has patterns for "tools" discovery and agent creation.
- Network access to `https://learn.microsoft.com/api/mcp` is allowed from the service environment.
- ServiceDefaults and DI conventions follow other Infrastructure clients in `ObsidianAI.Infrastructure/DI` and `ObsidianAI.ServiceDefaults`.

Design contract (tiny)
- Input: configuration (base URL, optional API key), requests to list or call tools.
- Output: a typed client exposing methods: ListToolsAsync(), GetToolSpecAsync(toolId), CreateMcpClientTransport() or GetMcpClientAsync().
- Errors: surface transient HTTP failures as retriable exceptions; provide cancellation tokens and clear logging.

Edge cases
- Microsoft Learn MCP endpoint returns non-200 or rate-limits: implement retries with exponential backoff and log metrics.
- Malformed tool metadata: fail gracefully and skip invalid tools when merging.
- Partial failures: return best-effort tools list; agent should handle missing capabilities.

Phase 1 — API contract & configuration (docs + code sketch)
Files and changes
- New: `ObsidianAI.Infrastructure/Vault/IMicrosoftLearnMcpClient.cs` (interface)
- New: `ObsidianAI.Infrastructure/Vault/MicrosoftLearnMcpClient.cs` (implementation)
- New: `ObsidianAI.Infrastructure/Configuration/MicrosoftLearnMcpOptions.cs` (POCO for binding)

Configuration POCO example
```csharp
public class MicrosoftLearnMcpOptions
{
      public const string Section = "MicrosoftLearnMcp";
      public string BaseUrl { get; set; } = "https://learn.microsoft.com/api/mcp";
      public string? ApiKey { get; set; }
      public int TimeoutSeconds { get; set; } = 30;
}
```

Interface sketch
```csharp
public interface IMicrosoftLearnMcpClient
{
      Task<IEnumerable<ToolDescriptor>> ListToolsAsync(CancellationToken ct = default);
      Task<ToolDescriptor?> GetToolAsync(string toolId, CancellationToken ct = default);
      /// <summary>Returns a ready-to-use MCP client wrapper for advanced scenarios.</summary>
      Task<McpClient> CreateMcpClientAsync(CancellationToken ct = default);
}
```

Implementation notes
- Use IHttpClientFactory to create named HttpClient (e.g. "MicrosoftLearnMcp").
- Configure HttpClient in DI with base address and timeout from MicrosoftLearnMcpOptions.
- Use Polly (if already used in repository) for retry and circuit-breaker policies. If Polly isn't present, implement basic retry with exponential backoff.
- Parse MCP tool list and map into your ToolDescriptor used by agent code. Validate required fields.

DI registration (example)
```csharp
services.Configure<MicrosoftLearnMcpOptions>(configuration.GetSection(MicrosoftLearnMcpOptions.Section));
services.AddHttpClient("MicrosoftLearnMcp", (sp, client) => {
      var opts = sp.GetRequiredService<IOptions<MicrosoftLearnMcpOptions>>().Value;
      client.BaseAddress = new Uri(opts.BaseUrl);
      client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
      if (!string.IsNullOrEmpty(opts.ApiKey)) client.DefaultRequestHeaders.Add("Authorization", $"Bearer {opts.ApiKey}");
})
// optional: .AddPolicyHandler(...polly policies...)
;
services.AddSingleton<IMicrosoftLearnMcpClient, MicrosoftLearnMcpClient>();
```

Phase 2 — Client implementation (detailed)
- Use HttpClient to POST/GET MCP messages. The MCP spec uses JSON-RPC like format; at minimum implement `tools/list` and `tools/get` requests.
- Provide cancellation tokens, logging, and metrics via ILogger and IMetrics (if available).
- Map responses to a `ToolDescriptor` DTO that matches your agent tooling model.

Minimal request example (pseudocode)
```csharp
var payload = new { @event = "tools/list", // or method: 'tools.list'
      // include any required auth or discovery fields
 };
var resp = await httpClient.PostAsJsonAsync("/", payload, ct);
resp.EnsureSuccessStatusCode();
var json = await resp.Content.ReadFromJsonAsync<McpToolsListResponse>(cancellationToken: ct);
```

Phase 3 — Agent integration (Api project)
Files to edit
- `ObsidianAI.Api/Services/ObsidianAssistantService.cs` (or the service responsible for initializing agents)

Code changes
- Add `IMicrosoftLearnMcpClient` to the service constructor and store in the class.
- In agent initialization (e.g., `InitializeAgentAsync`):
   - Call `ListToolsAsync()` on the Microsoft Learn client. Validate and transform to the agent's tool model.
   - Merge: `var allTools = obsidianTools.Concat(msLearnToolsWhereValid)` with de-dup by tool id or capability.
   - Pass merged tools to agent factory or CreateAIAgent method.

Example merge snippet
```csharp
var msTools = (await _msLearnClient.ListToolsAsync(ct)).Where(t => t.IsValid());
var allTools = obsidianTools.Concat(msTools).GroupBy(t => t.Id).Select(g => g.First());
var agent = _agentFactory.CreateAIAgent(allTools);
```

Update system instructions
- Add a short section to the agent system prompt explaining the Microsoft Learn tools and when to prefer them. Example:
```
MICROSOFT LEARN TOOLS:
- microsoft_docs_search — search Microsoft Learn docs for keywords and topics.
- microsoft_docs_fetch — retrieve the full article content for a doc id/url.
- microsoft_code_sample_search — locate code samples and snippets in docs.

Use these for authorative Microsoft documentation, API references, and sample code for Microsoft platforms (.NET, Azure, Power Platform, etc.). Prefer Microsoft Learn when answers require official docs or code examples.
```

Phase 4 — Health checks & observability
Files and steps
- New: `ObsidianAI.Api/HealthChecks/MicrosoftLearnHealthCheck.cs` implementing IHealthCheck.
- Register in `EndpointRegistration` or wherever health checks are registered: `.AddCheck<MicrosoftLearnHealthCheck>("microsoft-learn")`.

HealthCheck behaviour
- Perform a small `tools/list` request and consider success on a 200 with parsable tool list.
- Implement short-circuit on repeated failures and include response time metric.

Logging & metrics
- Log at INFO when tools successfully loaded with count, and WARN/ERROR on failure including HTTP status and truncated body.
- Optionally emit a metric `mcp.microsoftlearn.tools_loaded` with value = toolCount.

Phase 5 — Tests
Unit tests
- Add unit tests under `ObsidianAI.Tests/Infrastructure` for `MicrosoftLearnMcpClient` using HttpMessageHandler mocking (e.g., `HttpMessageHandler`/`HttpClient` test handler) to simulate:
   - tools/list success response
   - malformed response -> client gracefully handles and returns empty list
   - transient 429/5xx -> retry triggers (if using Polly ensure policy exercised)

Integration tests
- Add an integration test to `ObsidianAI.Tests/Application` (or similar) that wires a test `IMicrosoftLearnMcpClient` (mock or fake) into the `ObsidianAssistantService` and verifies tools are merged and the agent factory gets expected tools.

Sample unit test outline (pseudo)
```csharp
[Fact]
public async Task ListTools_ReturnsTools_WhenApiOk()
{
      var handler = new FakeHttpMessageHandler(200, jsonPayload);
      var client = new HttpClient(handler) { BaseAddress = new Uri("https://learn.microsoft.com/") };
      var svc = new MicrosoftLearnMcpClient(client, ...);
      var tools = await svc.ListToolsAsync();
      Assert.NotEmpty(tools);
}
```

Phase 6 — Rollout and verification
Staged rollout checklist
1. Feature branch with changes and unit tests passing.
2. Run `dotnet test` locally and ensure no regressions in `ObsidianAI.Tests`.
3. Deploy to a staging environment with network access to Microsoft Learn.
4. Smoke tests: call agent with queries that should trigger Microsoft Learn tool usage (example: "Find code sample for Az.ResourceManager VirtualMachine"), observe logs for "Loaded X Obsidian + Y Microsoft Learn tools".
5. Monitor health check `microsoft-learn` and watch for errors.
6. After 24-72 hours of stable behaviour, promote to production.

Rollback plan
- Feature flags: if runtime feature flagging exists for tools, toggle off the Microsoft Learn toolset.
- If not, rollback the service to the last successful deployment.

Timeline (estimate)
- Design & doc: 1 day (this doc)
- Implementation: 1-2 days (client + DI + agent merge)
- Tests & health checks: 0.5-1 day
- Staging verification & rollout: 0.5-1 day

Risks & mitigations
- Rate limiting or unexpected API contract changes from Microsoft Learn: add retry/backoff and fail-open (agents continue with Obsidian tools if MS Learn is unavailable).
- Sensitive or copyrighted content: Microsoft Learn is public docs; still ensure the agent cites sources and returns URLs.
- Added maintenance burden: keep the client small and well-tested.

Deliverables summary
- Files to add
   - `ObsidianAI.Infrastructure/Vault/IMicrosoftLearnMcpClient.cs`
   - `ObsidianAI.Infrastructure/Vault/MicrosoftLearnMcpClient.cs`
   - `ObsidianAI.Infrastructure/Configuration/MicrosoftLearnMcpOptions.cs`
   - `ObsidianAI.Api/HealthChecks/MicrosoftLearnHealthCheck.cs`
- Files to modify
   - `ObsidianAI.Api/Services/ObsidianAssistantService.cs` (or agent initializer)
   - DI registration file in `ObsidianAI.Infrastructure/DI` or `ObsidianAI.ServiceDefaults/Extensions.cs` as appropriate
- Tests to add
   - Unit tests for client (happy + error paths)
   - Integration test for agent merge

Verification checklist (to mark before merge)
- [ ] Unit tests for client pass locally
- [ ] Integration test for agent merge passes
- [ ] Health check registered and green in staging
- [ ] Logs show Microsoft Learn tools loaded count
- [ ] System prompt includes clear guidance for Microsoft Learn tool usage

Notes and follow-ups
- If you already have a generic MCP transport helper in the repo (e.g., an existing HttpClientTransport wrapper), reuse it to avoid duplication.
- Consider adding a feature flag around Microsoft Learn tools for safer rollout.
- If you want I can scaffold the actual code files and unit tests in a follow-up change.

---

End of plan.