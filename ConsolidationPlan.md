# Consolidation Plan — ChatAgent-only architecture (refined)

This document describes the updated plan to replace the six-class approach (3 low-level HTTP clients + 3 agents) with a single agent-per-provider design. It incorporates your refinements: keyed DI, factory behavior change, BaseChatAgent not implementing IChatAgent, explicit CallAsync mapping, model-discovery option, testing order, verification step, metrics preservation, feature-flagged rollout, and provider onboarding docs.

---

## Summary (one line)
Replace OpenRouterClient / LMStudioClient / NanoGptClient with provider ChatAgent implementations only; extract shared logic to BaseChatAgent (abstract), register agents as keyed services, and update AIClientFactory to resolve concrete agents by provider key.

---

## Deliverables
- New: `ObsidianAI.Infrastructure\LLM\BaseChatAgent.cs` (abstract, protected helpers)
- Updated: `OpenRouterChatAgent.cs`, `LmStudioChatAgent.cs`, `NanoGptChatAgent.cs` (inherit BaseChatAgent; implement IAIClient in provider classes)
- Updated: `ObsidianAI.Infrastructure\LLM\Factories\AIClientFactory.cs` (resolve concrete agents by provider key)
- Updated DI: `ObsidianAI.Infrastructure\LLM\DependencyInjection.cs` (register keyed services)
- New doc: `ADDING_PROVIDERS.md`
- Tests: unit tests for BaseChatAgent, integration tests for AIProvider and chat flows
- Optional: feature flag `UseModernAgentClients` to toggle use of agent-backed IAIClient

---

## Detailed Plan

### 1) DI: keyed services (required)
Register concrete agents and keyed IChatAgent mappings:

- Register concrete agent types
  - services.AddScoped<OpenRouterChatAgent>();
  - services.AddScoped<LmStudioChatAgent>();
  - services.AddScoped<NanoGptChatAgent>();

- Register keyed IChatAgent services (example API your app uses)
  - services.AddKeyedScoped<IChatAgent>("OpenRouter", (sp, key) => sp.GetRequiredService<OpenRouterChatAgent>());
  - services.AddKeyedScoped<IChatAgent>("LMStudio",  (sp, key) => sp.GetRequiredService<LmStudioChatAgent>())
  - services.AddKeyedScoped<IChatAgent>("NanoGPT",  (sp, key) => sp.GetRequiredService<NanoGptChatAgent>());

Note: keep concrete types available for factory resolution and for registering IAIClient behavior on the same instance.

---

### 2) AIClientFactory behavior (change)
- Stop enumerating `IEnumerable<IAIClient>`.
- Use a service-locator style resolution by provider name (map to concrete agent type):
  - If providerName == "OpenRouter" ? sp.GetRequiredService<OpenRouterChatAgent>()
  - If providerName == "LMStudio" ? sp.GetRequiredService<LmStudioChatAgent>()
  - If providerName == "NanoGPT" ? sp.GetRequiredService<NanoGptChatAgent>()
- Return the concrete instance cast to IAIClient (providers will implement IAIClient).
- This yields one instance per scope and aligns with keyed DI.

Example (pseudocode):
```
GetClient(providerName):
  - if "OpenRouter" ? sp.GetRequiredService<OpenRouterChatAgent>()
  - if "LMStudio" ? sp.GetRequiredService<LmStudioChatAgent>()
  - if "NanoGPT" ? sp.GetRequiredService<NanoGptChatAgent>()

This ensures single instance per provider per scope.
```

---

### 3) BaseChatAgent (abstract) — responsibilities (must NOT implement IChatAgent)
- Keep BaseChatAgent abstract and provide protected members/utilities:
  - protected readonly IChatClient _chatClient;
  - protected readonly ChatClientAgent _agent;
  - Protected streaming pipeline implementation method(s) (callable by providers).
  - Helpers:
    - Tool handling: Format tool call/result payloads (FunctionCallContent / FunctionResultContent mapping).
    - ExtractActionCardFromToolResult(result)
    - ResolveToolName(object source, string fallback = "unknown")
    - Usage extraction helper (reflective, tolerant)
    - Common cancellation/exception handling pattern
- Do NOT implement IChatAgent on BaseChatAgent. Each provider explicitly implements IChatAgent so it can customize behavior and override streaming or health logic as needed.

Rationale: provider-specific quirks (HTTP shapes, rate-limit handling, health endpoints) can be handled in provider classes.

---

### 4) Provider classes — responsibilities & IAIClient mapping
- Each provider class (OpenRouterChatAgent, LmStudioChatAgent, NanoGptChatAgent):
  - Inherit BaseChatAgent.
  - Implement IChatAgent (SendAsync, StreamAsync, CreateThreadAsync) — call into BaseChatAgent helpers as appropriate.
  - Implement IAIClient:
    - CallAsync(AIRequest request, CancellationToken ct)
      - Convert domain AIRequest -> List<ChatMessage> (Microsoft.Extensions.AI ChatMessage)
        - System message from request.SystemMessage (if present)
        - User message from request.Prompt
      - Call _chatClient.CompleteAsync(messages) or .RunAsync equivalent (single-shot) via the agent SDK
      - Map the returned response -> domain AIResponse:
        - Content <- response.Text (or first assistant message)
        - Model  <- configured provider model
        - TokensUsed <- extract from response.Usage via helper
        - ProviderName <- provider name
    - IsHealthyAsync(CancellationToken ct)
      - Lightweight check: prefer SDK metadata if available; or small HTTP GET using a named HttpClient created via IHttpClientFactory.
    - GetModelsAsync(CancellationToken ct)
      - Option A (recommended simple): return configured model name only (via settings or _chatClient metadata).
      - If full model list required, implement a small per-provider HTTP helper inside the provider (use IHttpClientFactory, not a full old client class).

Notes:
- Keep domain model mapping inside these provider classes (AIRequest -> ChatMessage) so Domain layer unchanged.
- Preserve usage metadata format via helper functions so AIProvider behavior remains stable.

---

### 5) Migration steps (strict order)
1. Add `BaseChatAgent` (implement protected pipeline helpers and unit-testable methods).
2. Update `OpenRouterChatAgent` to:
   - inherit BaseChatAgent,
   - implement `IChatAgent` (existing) and `IAIClient` (new),
   - wire CallAsync/IsHealthyAsync/GetModelsAsync per spec.
3. Repeat for `LmStudioChatAgent` and `NanoGptChatAgent`.
4. Add keyed DI registrations (services.AddScoped concrete agents + AddKeyedScoped mappings).
5. Update `AIClientFactory` to resolve concrete agents via provider name (service locator style).
6. Keep legacy Client classes in-place but do NOT remove yet. Add feature flag `UseModernAgentClients` in configuration and route AIProvider to agent-backed IAIClient only when flag true.
7. Run tests & manual checks; after verification, remove legacy client classes and old DI registrations.
8. Finalize by deleting clients once zero references confirmed.

---

### 6) Verification step (explicit — DO BEFORE DELETE)
- For each legacy client file:
  - Run "Find All References" / project-wide search on class name (OpenRouterClient, LMStudioClient, NanoGptClient).
  - Confirm only DI registration and tests reference them (or none).
  - If any additional references exist, update callers to use agent or adapter first.
- Only after zero references and tests passing, delete files.

---

### 7) Testing priority (order)
1. Unit tests for BaseChatAgent (isolated streaming & tools parsing).
2. Unit tests for provider CallAsync mapping (fake/mocked IChatClient returning expected shapes).
3. Integration tests for AIProvider (IAIClient contract: CallAsync, fallback behavior).
4. Integration tests for chat flows (IChatAgent contract: streaming, tool execution).
5. Manual UI smoke tests (Blazor chat streaming + tool confirmations).

---

### 8) Metrics & usage preservation (explicit checks)
Before/after validation:
- Token counts (input/output/total) captured and consistent.
- Usage metadata payload shape preserved or mapped to previous format (if domain expects a particular shape).
- Health check responses still behave the same for health endpoints.

Add automated assertions in integration tests to compare token fields when possible.

---

### 9) Feature flag (recommended rollback approach)
- Add config flag `UseModernAgentClients` (default true in new deployments; default false during staged rollout).
- When false: AIProvider uses legacy IAIClientFactory behavior (existing HTTP clients).
- When true: AIProvider uses agent-backed clients via updated factory.
- Keep legacy clients around for a short validation period.
- Rollback: toggle flag to false without code revert.

---

### 10) Model discovery (GetModelsAsync) strategy
- Option A (SIMPLE, recommended): return configured model name only (via settings or _chatClient metadata).
  - Pros: no HTTP calls, minimal churn.
  - Cons: does not enumerate remote provider models.
- Option B (COMPREHENSIVE): per-provider small HTTP helper using IHttpClientFactory to call provider models endpoint; implement only when UI requires full list.

---

### 11) ADDING_PROVIDERS.md (deliverable)
- Template describing how to add a new provider:
  - Add provider settings to AppSettings.LLM
  - Add provider concrete class inheriting BaseChatAgent
  - Register concrete provider in DI and AddKeyedScoped mapping
  - Implement IAIClient methods, prefer Option A for GetModelsAsync
  - Tests checklist
  - Example code snippets
- Include DI checklist and which code belongs in BaseChatAgent vs provider.

---

## Timeline estimate (single developer)
- Design & create BaseChatAgent: 0.5–1 day
- Implement OpenRouter agent changes + unit tests: 0.5–1 day
- Implement LMStudio + NanoGPT changes: 0.5–1 day
- DI + Factory updates + integration tests: 0.5 day
- Staging verification, manual UI tests, metrics checks: 0.5 day
- Cleanup and remove legacy clients after validation: 0.25 day
Estimated total: ~3–4 days

---

## Rollback & risk mitigation
- Use `UseModernAgentClients` feature flag for safe rollback without code reverts.
- Keep legacy clients in codebase for a short validation period.
- Ensure tests to compare token/usage behavior.

---

## Minimal code snippets (for reference)

DI keyed registration (DependencyInjection.cs):
```csharp
// Register concrete agents
services.AddScoped<OpenRouterChatAgent>();
services.AddScoped<LmStudioChatAgent>();
services.AddScoped<NanoGptChatAgent>();

// Register keyed IChatAgent
services.AddKeyedScoped<IChatAgent>("OpenRouter", (sp, key) => sp.GetRequiredService<OpenRouterChatAgent>());
services.AddKeyedScoped<IChatAgent>("LMStudio",  (sp, key) => sp.GetRequiredService<LmStudioChatAgent>());
services.AddKeyedScoped<IChatAgent>("NanoGPT",  (sp, key) => sp.GetRequiredService<NanoGptChatAgent>());
```

AIClientFactory resolve example (pseudocode):
```csharp
public IAIClient? GetClient(string providerName)
{
    return providerName switch
    {
        "OpenRouter" => _serviceProvider.GetRequiredService<OpenRouterChatAgent>(),
        "LMStudio"   => _serviceProvider.GetRequiredService<LmStudioChatAgent>(),
        "NanoGPT"    => _serviceProvider.GetRequiredService<NanoGptChatAgent>(),
        _ => null
    };
}
```

CallAsync mapping (provider class):
```csharp
public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken ct = default)
{
    var messages = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.System, request.SystemMessage ?? ""),
        new ChatMessage(ChatRole.User, request.Prompt)
    };

    var response = await _chatClient.CompleteAsync(messages, cancellationToken: ct); // SDK API
    var text = response.Text ?? string.Empty;
    var tokens = UsageHelper.ExtractTotalTokens(response.Usage);
    return new AIResponse { Content = text.Trim(), Model = _configuredModel, TokensUsed = tokens, ProviderName = ProviderName };
}
```

---

## Final notes
- This plan preserves the Domain layer; mapping from domain AIRequest ? framework ChatMessage happens inside provider classes only.
- BaseChatAgent remains abstract (no IChatAgent implementation) to allow provider overrides for provider-specific behavior.
- Feature flag approach makes rollout reversible without code revert and reduces operational risk.

If you want, I will:
- Scaffold `BaseChatAgent` and update `OpenRouterChatAgent` to implement `IAIClient` and call out the exact edits (diffs). Which provider do you want implemented first for the concrete code changes?