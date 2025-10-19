Repository analysis and pattern extraction

Overview of MCP usage in this repo
- Central MCP gateway orchestration:
  - Compose: [docker-compose.yml](docker-compose.yml) sets MCP_ENDPOINT and starts the official gateway image with --servers obsidian, exposing http://mcp-gateway:8033/mcp, consumed by Web via MCP_ENDPOINT.
  - Aspire: [ObsidianAI.AppHost/AppHost.cs](ObsidianAI.AppHost/AppHost.cs) boots a docker-based gateway process and injects MCP_ENDPOINT to the Web app.
- Client pattern (Web/API):
  - Shared lazy client for the “vault/obsidian” server: [ObsidianAI.Web/Services/McpClientService.cs](ObsidianAI.Web/Services/McpClientService.cs) creates a single HttpClientTransport to MCP gateway using MCP_ENDPOINT and exposes IMcpClientProvider for discovery and tool invocation. Logging via ILogger, resilient initialization with Lazy<Task>.
  - Dedicated client for Microsoft Learn MCP: [ObsidianAI.Web/Services/MicrosoftLearnMcpClient.cs](ObsidianAI.Web/Services/MicrosoftLearnMcpClient.cs) uses IConfiguration + env var MICROSOFT_LEARN_MCP_ENDPOINT to create a transport per endpoint (direct connection) and exposes IMicrosoftLearnMcpClientProvider. Synchronization via SemaphoreSlim, logs, graceful null returns if not configured or unreachable.
- Tool discovery and merging:
  - IMcpToolCatalog merges tools across Obsidian vault MCP and Microsoft Learn MCP, caches them with TTL and logs counts: [ObsidianAI.Application/Services/McpToolCatalog.cs](ObsidianAI.Application/Services/McpToolCatalog.cs).
  - Use cases load catalog snapshot to populate LLM tool lists: [ObsidianAI.Application/UseCases/StartChatUseCase.cs](ObsidianAI.Application/UseCases/StartChatUseCase.cs), [ObsidianAI.Application/UseCases/StreamChatUseCase.cs](ObsidianAI.Application/UseCases/StreamChatUseCase.cs).
- Vault operations are backed by MCP tools:
  - Read/list/search: [ObsidianAI.Application/UseCases/ListVaultContentsUseCase.cs](ObsidianAI.Application/UseCases/ListVaultContentsUseCase.cs), [ObsidianAI.Application/UseCases/ReadFileUseCase.cs](ObsidianAI.Application/UseCases/ReadFileUseCase.cs).
  - Write operations executed via IVaultToolExecutor with an MCP-backed implementation [ObsidianAI.Infrastructure/Vault/McpVaultToolExecutor.cs](ObsidianAI.Infrastructure/Vault/McpVaultToolExecutor.cs) and a Null executor fallback [ObsidianAI.Infrastructure/Vault/NullVaultToolExecutor.cs](ObsidianAI.Infrastructure/Vault/NullVaultToolExecutor.cs).
- Health checks:
  - Gateway connectivity check uses IMcpClientProvider and ListToolsAsync: [ObsidianAI.Infrastructure/HealthChecks/McpHealthCheck.cs](ObsidianAI.Infrastructure/HealthChecks/McpHealthCheck.cs).
  - Microsoft Learn endpoint specific check: [ObsidianAI.Web/HealthChecks/MicrosoftLearnHealthCheck.cs](ObsidianAI.Web/HealthChecks/MicrosoftLearnHealthCheck.cs).
- Configuration:
  - MCP gateway endpoint: env var MCP_ENDPOINT injected into the web container and Aspire project. Microsoft Learn endpoint via appsettings.json key MicrosoftLearnMcp:Endpoint or env MICROSOFT_LEARN_MCP_ENDPOINT: [ObsidianAI.Web/appsettings.json](ObsidianAI.Web/appsettings.json).
- Logging/telemetry:
  - Extensive ILogger usage across services. Streaming tool invocation counter in [ObsidianAI.Web/Streaming/StreamingEventWriter.cs](ObsidianAI.Web/Streaming/StreamingEventWriter.cs:16). Health check logs. No explicit OpenTelemetry configured for MCP clients; .NET defaults with structured logging used throughout.
- Error handling patterns:
  - Clients return null when not configured or unreachable. Use cases guard against null client with graceful fallbacks. For tool invocation failures, errors mapped to OperationResult or InvalidOperationException in read path. Global exception middleware: [ObsidianAI.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs](ObsidianAI.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs:1) registered via [ObsidianAI.Web/Program.cs](ObsidianAI.Web/Program.cs:143).
- Naming & boundaries:
  - Interfaces live in Application (IMcpClientProvider, IMicrosoftLearnMcpClientProvider, IMcpToolCatalog). Infrastructure wires implementations via DI. API/Web registers health checks and hosted services. All MCP consumption is from Web/Application, server code is not present here.

Conclusion: The repository contains MCP consumers (clients, health, tool catalog) but no MCP server code. The “Microsoft Learn MCP Server” is an external endpoint (https://learn.microsoft.com/api/mcp). The “Obsidian Vault MCP Server” is provided to the gateway via --servers obsidian, also external to this repo.

Patterns to reuse, adapt, generalize
- Reuse verbatim:
  - Configuration conventions: environment-first with optional appsettings, e.g., MCP_ENDPOINT and MicrosoftLearnMcp:Endpoint style keys.
  - Health check behavior: simple reachability via tools.list call and classify Healthy/Degraded/Unhealthy with clear messages.
  - Tool catalog merging and TTL caching approach for combining servers’ tools.
  - Logging level conventions (Information for success with counts; Warning for recoverable failures; Error for startup failures).
  - Lazy/singleton client provisioning pattern exposed as an interface provider (for a .NET client). For the new server, we mirror developer experience by offering a clear, minimal client surface.
- Adapt:
  - Error taxonomy: today it’s ad-hoc; adopt a shared, explicit error code set for MCP tool failures and propagate through OperationResult and HealthChecks, without breaking existing behavior. Introduce consistent mapping in McpVaultToolExecutor and MicrosoftLearn flows.
  - Telemetry: standardize counters and histograms for MCP operations (counts, latency, bytes) with correlation id propagation. Keep logging semantics but add metrics namespaced consistently.
  - Configuration modeling: extend conventions to a multi-root, sandboxed filesystem server. Keep env key style but add a simple JSON config file option when running under gateway.
- Generalize into shared modules (new small TS or schema package):
  - Common JSON Schema fragments for tool request/response shapes (pagination, path, glob, encoding, error envelope).
  - Error code enum and mapping helpers.
  - Path normalization rules and sandbox verification logic (shared between handlers).
  - Metrics/logging helpers (trace context extraction from MCP request, standard attributes).

Parity mapping with Microsoft Learn and Obsidian Vault servers (prose)
- Discovery and Tool registration:
  - Microsoft Learn: tools exposed by remote endpoint; consumed through ListToolsAsync-like call; merged via IMcpToolCatalog.
  - Obsidian Vault: tools exposed by the “obsidian” server through gateway; discovered by ListToolsAsync.
  - FileSystem: tools exposed by our new server, discovered the same way and merged by IMcpToolCatalog automatically. No change required in consumers.
- Resource exposure:
  - Microsoft Learn: content fetched by tools (docs_fetch); no explicit resources enumerated through resource.list in this repo’s client.
  - Obsidian Vault: tools return lists of vault paths; again consumed via tool calls.
  - FileSystem: expose resources in addition to tools for uniformity: resource.list (fs://root-alias/...), resource.read with encoding options. This extends parity beyond current usage but stays MCP-compliant.
- Auth/config:
  - Microsoft Learn: endpoint URL config with optional auth key in docs plan [docs/Microsoft-Learn-MCP.md](docs/Microsoft-Learn-MCP.md:33).
  - Obsidian: configured into gateway via --servers obsidian.
  - FileSystem: server-local config (env or config file) for sandbox roots, read-only mode, ignore rules. The gateway will run the server with env vars. No client-side new settings are required to consume.
- Errors:
  - Current: exceptions logged, InvalidOperationException thrown for read failures; OperationResult for write flows; health checks report Unhealthy/Degraded.
  - FileSystem: map errors to a stable taxonomy (FS_NOT_FOUND, FS_FORBIDDEN, etc.), return consistent MCP error envelopes; on .NET consumer side we maintain the same handling pattern but enrich logs/metrics.
- Logging/metrics:
  - Current: ILogger with counts printed, one streaming counter exists.
  - FileSystem: structured logs with correlation IDs (if gateway passes), guard debug traces by config, and metrics aligned to naming pattern mcp.* (e.g., mcp.filesystem.op.count).

FileSystem server scope and capabilities

Primary goal
Safe, sandboxed, cross-platform file and directory operations via MCP tools and resources, rooted in an allowlist of base paths. Defaults to strict read-only.

Capabilities
- Path and stat:
  - filesystem.stat: file/dir existence, size, mode, times, type
  - filesystem.normalize: normalize and resolve relative segments safely within sandbox (no traversal outside)
- Listing:
  - filesystem.list: directory entries with pagination, filtering by type, glob patterns, max recursion depth
- Reading:
  - filesystem.read: read text/binary content with:
    - encoding options (utf-8 default, binary/raw, base64)
    - size caps and truncated flags
    - optional streaming (chunked) if MCP transport supports it
- Writing and appending (guarded by read-only / feature flag):
  - filesystem.write: atomic write (temp-and-rename), encoding handling, optional mode preservation
  - filesystem.append: append text/binary; same encodings
- Mutations (guarded by read-only / feature flag):
  - filesystem.create: create file or directory; file mode optional
  - filesystem.move/copy: conflict policy (fail/overwrite/skip), follow symlink policy
  - filesystem.delete: file or directory recursive; dry-run supported
- Search:
  - filesystem.search: glob search with optional content regex, case sensitivity switch, max-results cap, ignore patterns (.gitignore aware optional)
- Watchers (optional):
  - filesystem.watch.start / filesystem.watch.stop: file/directory change notifications where MCP supports notifications and cancellation; emits events with debounce/backpressure
- Cross-platform behavior:
  - Correct path separator handling, case sensitivity considerations, Windows long-path mitigation, symlink policies
- Concurrency/cancellation:
  - Per-root operation semaphores for mutations, cooperative cancellation, timeouts, internal backpressure
- Large file handling:
  - Size limits, content-type detection (MIME sniff), binary-safe operations
- Security:
  - Strict sandbox resolution, deny traversal outside roots, symlink policy (deny or resolve within root then verify), TOCTOU mitigations via open-by-handle and atomic replace where possible

Public API and schemas

Tools (names, summary, key params; all returned errors include error.code and error.message):
- filesystem.version
  - Returns server version, platform, capabilities, config flags (readOnly true/false).
  - Request: {}. Response: { version, platform, readOnly, features[] }.
- filesystem.stat
  - Request: { path, root?, followSymlinks?=false }
  - Response: { type: "file"|"directory"|"symlink"|"other", size, mode?, mtime?, ctime?, target? }
- filesystem.normalize
  - Request: { path, root? }
  - Response: { normalizedPath, root, isWithinSandbox: true|false }
- filesystem.list
  - Request: { path, root?, page?=1, pageSize?=100, types?=["file"|"directory"], glob?, includeDotFiles?=false, maxDepth?=0 }
  - Response: { items: [{ name, type, size?, mtime? }], page, pageSize, total, truncated: bool }
- filesystem.read
  - Request: { path, root?, encoding?="utf-8"|"base64"|"binary", maxBytes?=5_000_000, stream?=false }
  - Response (non-stream): { encoding, data, bytesRead, truncated }
  - Response (stream): starts stream of chunks { seq, encoding, data, eof? }
- filesystem.write (feature-flagged)
  - Request: { path, root?, encoding?="utf-8"|"base64"|"binary", data, preserveMode?=true, createParents?=true, overwrite?=true, atomic?=true, dryRun?=false }
  - Response: { success, bytesWritten?, mode? }
- filesystem.append (feature-flagged)
  - Request: { path, root?, encoding?="utf-8"|"base64"|"binary", data, createIfMissing?=false, dryRun?=false }
  - Response: { success, bytesAppended? }
- filesystem.create (feature-flagged)
  - Request: { path, root?, kind: "file"|"directory", mode?, createParents?=true, overwrite?=false, dryRun?=false }
  - Response: { success }
- filesystem.move / filesystem.copy (feature-flagged)
  - Request: { from, to, root?, overwrite?=false, conflictPolicy?="fail"|"overwrite"|"skip", followSymlinks?=false, dryRun?=false }
  - Response: { success, moved|copied: count }
- filesystem.delete (feature-flagged)
  - Request: { path, root?, recursive?=false, force?=false, dryRun?=false }
  - Response: { success, deletedCount }
- filesystem.search
  - Request: { root?, path, glob?="**/*", contentQuery?, caseSensitive?=false, maxResults?=1000, includeDotFiles?=false, respectGitignore?=true, ignorePatterns?[] }
  - Response: { results: [{ path, root, type, size?, matches?[] }], truncated }
- filesystem.watch.start / filesystem.watch.stop (optional)
  - start Request: { path, root?, recursive?=true, ignorePatterns?[], debounceMs?=100 }
  - start Response: { subscriptionId }
  - notifications: { subscriptionId, event: "create"|"modify"|"delete", path, type }
  - stop Request: { subscriptionId } -> Response: { success }

Resources
- resource.list
  - Exposes fs://{rootAlias}/ prefixes and top-level directories available. Response: [{ uri: "fs://rootAlias/", title, description }]
- resource.read
  - Accepts fs://rootAlias/path URIs and options { encoding?, maxBytes? } and returns content similar to filesystem.read.

Error taxonomy
- Codes (mapped to MCP error envelopes):
  - FS_BAD_REQUEST (invalid arguments)
  - FS_FORBIDDEN (outside sandbox, policy violation, read-only)
  - FS_NOT_FOUND
  - FS_CONFLICT (exists/overwrite policy)
  - FS_TOO_LARGE (beyond caps)
  - FS_UNSUPPORTED (operation unsupported on platform)
  - FS_TIMEOUT
  - FS_IO_ERROR (generic I/O)
  - FS_TRAVERSAL_BLOCKED (normalized path escapes root)
  - FS_SYMLINK_POLICY (symlink policy denied)
- Mapping in consumers:
  - Read flows may raise InvalidOperationException with message including mapped code; write flows return OperationResult(false, message, path) maintaining existing behavior while including code in logs/metrics.

Configuration surface

Environment variables (server process)
- FILESYSTEM_MCP__ROOTS: semicolon-separated list of “alias=absolutePath” (required). Example: “workspace=C:\git\ObsidianAI;home=C:\Users\user”.
- FILESYSTEM_MCP__READ_ONLY: true (default) | false.
- FILESYSTEM_MCP__IGNORE_PATTERNS: JSON array or semicolon-separated globs (default: empty).
- FILESYSTEM_MCP__RESPECT_GITIGNORE: true (default).
- FILESYSTEM_MCP__MAX_READ_BYTES: 5000000.
- FILESYSTEM_MCP__MAX_LIST_PAGE_SIZE: 500.
- FILESYSTEM_MCP__MAX_RECURSION_DEPTH: 5.
- FILESYSTEM_MCP__DEFAULT_ENCODING: utf-8.
- FILESYSTEM_MCP__ENABLE_WATCHERS: false (default).
- FILESYSTEM_MCP__SYMLINK_POLICY: "deny" (default) | "follow-inside-root".
- FILESYSTEM_MCP__TIMEOUT_MS: 10000.
- FILESYSTEM_MCP__LOG_LEVEL: info|debug.
- FILESYSTEM_MCP__OTEL_EXPORTER_OTLP_ENDPOINT: optional for OpenTelemetry.
- FILESYSTEM_MCP__RATE_LIMIT_RPS: optional per-root rate cap for mutations.

Optional config file (JSON)
- FILESYSTEM_MCP__CONFIG points to a JSON file with equivalent keys to support complex setups.

Security model

Threat model and mitigations
- Path traversal/normalization pitfalls:
  - Always resolve candidate path as rootAbs + normalized(relative), verify resulting absolute path startsWith rootAbs (case-insensitive on Windows). Deny otherwise with FS_TRAVERSAL_BLOCKED.
- Symlink races and TOCTOU:
  - Default policy deny symlinks for mutations. On read/list, optionally follow-inside-root, but after resolution re-verify resides within sandbox.
  - Use atomic write (temp in same directory + fs.rename) to minimize TOCTOU. Prefer open flags that avoid following symlinks if supported.
- Untrusted file names:
  - Validate Unicode normalization, reject control chars, null bytes; log sanitized paths only.
- Secret leakage and logs:
  - Redact absolute paths beyond root alias in logs; include only alias + relative. Clamp data logs; never log file content.
- Resource exhaustion:
  - Listing pagination and recursion caps; glob pre-filtering; maxResults and maxBytes; timeouts and cancellation; backpressure on watchers with debounce.
- Binary/special files:
  - Detect type via ext/MIME sniff; prohibit operations on device/special files if encountered; treat binary-safe mode as base64 only.
- Permission errors:
  - Map to FS_FORBIDDEN; in read-only mode, any mutating tool returns FS_FORBIDDEN.
- Platform-specific quirks:
  - Case sensitivity handling; Windows long path prefix support; separator normalization; reserved filenames blocked on Windows.

Architecture and module layout

Language/stack assumption
- TypeScript Node 18+, using @modelcontextprotocol/sdk, pnpm or npm workspaces (local package).
- Rationale: aligns with the broader MCP server ecosystem; repo currently has no TS workspace, so we’ll add a standalone servers package with its own package.json. This mirrors the external-servers model while keeping code co-located.

Directory structure (new)
- servers/filesystem-mcp/
  - package.json, tsconfig.json, README.md
  - src/
    - index.ts (bootstrap + server lifecycle)
    - config.ts (load/validate config from env/file)
    - logger.ts (pino/winston, correlation id support if headers present)
    - telemetry.ts (optional OpenTelemetry setup)
    - errors.ts (taxonomy + helpers)
    - sandbox.ts (path normalization, symlink policy, guard helpers)
    - encoding.ts (encoding handlers)
    - pagination.ts (list helpers)
    - handlers/
      - version.ts
      - stat.ts
      - normalize.ts
      - list.ts
      - read.ts
      - write.ts
      - append.ts
      - create.ts
      - move.ts
      - copy.ts
      - delete.ts
      - search.ts
      - watch.ts (optional)
    - resources/
      - list.ts
      - read.ts
  - test/ (jest or vitest)
    - unit/ (path normalization, sandbox, encoding, error mapping)
    - integration/ (temp directories for cross-platform)
    - e2e/ (MCP client harness)

Key runtime entrypoints and constructs (to be implemented)
- [TypeScript.bootstrapServer()](servers/filesystem-mcp/src/index.ts:1) – initializes SDK server, registers tools/resources/prompts, starts event loop, wires cancellation.
- [TypeScript.registerFilesystemTools()](servers/filesystem-mcp/src/index.ts:1) – attaches tool handlers (list, read, write, etc.) with JSON Schema validation per tool.
- [TypeScript.resolveSandboxedPath()](servers/filesystem-mcp/src/sandbox.ts:1) – normalized path resolution within configured root alias, enforces policies.
- [TypeScript.mapFsErrorToMcp()](servers/filesystem-mcp/src/errors.ts:1) – maps Node fs errors to error taxonomy.

Phased implementation plan with tasks and acceptance criteria

Phase 0 — Scaffolding and project setup
- Tasks:
  - Create servers/filesystem-mcp skeleton with package.json (name @obsidianai/filesystem-mcp, engines Node >=18, dependencies @modelcontextprotocol/sdk, chokidar, fast-glob, mime, pino or winston, zod/ajv for schema validation, optional @opentelemetry/api/sdk).
  - Add tsconfig, lint (eslint), test (vitest/jest) config. Add build script (tsc).
  - Add .github/workflows/mcp-filesystem.yml with Node matrix (16/18/20? choose 18+/20) – lint, typecheck, test, build, publish (GitHub Packages or npm per org policy).
- Deliverables: compiles, tests pass (empty), workflow green.
- Acceptance: CI passes; server boots to a no-op version tool locally via node dist/index.js.

Phase 1 — Configuration and sandbox foundation
- Tasks:
  - Implement [TypeScript.loadConfig()](servers/filesystem-mcp/src/config.ts:1) to process env and optional JSON file; validate with zod/ajv; defaults to read-only.
  - Implement [TypeScript.resolveSandboxedPath()](servers/filesystem-mcp/src/sandbox.ts:1) with strict checks; unit tests including traversal and symlink cases.
- Deliverables: unit tests for normalization, traversal blocking, symlink policy.
- Acceptance: property-based tests produce no escapes; 100% of cases within limits blocked/outcomes expected.

Phase 2 — Read-only operations
- Tasks:
  - Implement tools: version, normalize, stat, list, read, search. Add resource.list/resource.read.
  - Schemas: define JSON Schemas for each tool’s input/output; validate in handlers; enforce caps/timeouts.
  - Cross-platform and encoding handling; binary-safe reads with base64.
- Deliverables: integration tests with temp sandbox; performance baseline on directory walks; E2E via MCP client harness.
- Acceptance: health/version tools return OK; list/read/search parity with expected semantics; all tests green on Windows/Linux runners.

Phase 3 — Write operations with safety
- Tasks:
  - Implement write, append, create, move, copy, delete; all guarded by read-only flag and feature flags; atomic temp-and-rename semantics.
  - Conflict policies, overwrite flags, dry-run mode; mode preservation when applicable.
- Deliverables: unit and integration tests for all operations; race condition tests best-effort.
- Acceptance: read-only mode rejects mutations with FS_FORBIDDEN; write mode passes tests and preserves atomicity.

Phase 4 — Watchers and notifications (optional)
- Tasks:
  - If enabled, chokidar-based watchers; start/stop tool handlers; notification events with debounce/backpressure and cancellation.
- Deliverables: flaky-resistant tests where feasible, or manual verification instructions.
- Acceptance: notifications observed in harness; disabled by default.

Phase 5 — Telemetry and hardening
- Tasks:
  - Structured logging with correlation id extraction; counters/histograms per op: mcp.filesystem.op.count, mcp.filesystem.op.latency_ms, mcp.filesystem.bytes_read, bytes_written, errors.count with code labels.
  - OpenTelemetry optional exporter; toggle via env.
  - Fuzz/property tests for path inputs; large directory/large file perf tests with soft limits.
- Deliverables: docs for metrics, dashboards guidance.
- Acceptance: metrics emitted; error mapping verified across code paths.

Phase 6 — Documentation and examples
- Tasks:
  - Top-level README section additions, plus dedicated server docs in docs/mcp-filesystem.md with configuration, security notes, quickstart.
  - Usage examples mirroring Microsoft Learn docs style: tool invocation examples and resource URIs from .NET client perspective.
- Deliverables: docs PR with examples referencing MCP gateway integration.
- Acceptance: docs reviewed; quickstart reproducible locally.

Phase 7 — Integration and release
- Tasks:
  - Wire gateway to start filesystem server:
    - docker-compose: update gateway command to include the filesystem server via config file or env (see below).
    - Aspire AppHost: include server in args or gateway config resource.
  - Update health checks (optional): add a distinct filesystem health check by listing tools and verifying version tool invocation through the gateway.
- Deliverables: compose and Aspire updates; CI release workflow; tag/publish server package if desired.
- Acceptance: ObsidianAI can list filesystem tools via IMcpToolCatalog and call read/list successfully in staging.

Test strategy

Unit tests
- Path normalization and policy enforcement:
  - Randomized relative/absolute paths, Unicode, separator variations; assert expected normalized within alias.
  - Symlink policy: symlink into/outside root; ensure denied when configured.
- Encoding: decode/encode roundtrip, binary vs utf-8, truncation logic.
- Error taxonomy: map Node errors (ENOENT, EACCES, EPERM, EBUSY) to our FS_* codes.

Property-based/fuzz tests
- Generate random path inputs with traversal attempts (../, ..\, mixed Unicode); assert FS_TRAVERSAL_BLOCKED when appropriate.
- Fuzz glob patterns and content queries to ensure caps respected and timeouts enforced.

Integration tests
- Temp sandbox directories on each OS runner; create nested trees; test list/search/read; write operations under both read-only/rewrite modes.
- Windows long path tests when feasible; case sensitivity checks.

Performance tests
- Directory walks with 10k entries; measure latency and truncated flags.
- Large reads/writes with caps; backpressure validated.

End-to-end tests
- MCP client harness to call tools through a dev gateway instance; verify contract responses and event notifications (if watchers enabled).

Telemetry and observability

- Counters/histograms:
  - mcp.filesystem.op.count{op, root, status}, mcp.filesystem.op.latency_ms{op}, mcp.filesystem.bytes_read, mcp.filesystem.bytes_written, mcp.filesystem.errors.count{code}.
- Logging:
  - Request id/correlation id if present in MCP context headers; include root alias and normalized relative path; no content in logs.
- OpenTelemetry:
  - Optional Node SDK initialization in [TypeScript.setupTelemetry()](servers/filesystem-mcp/src/telemetry.ts:1) controlled via FILESYSTEM_MCP__OTEL_EXPORTER_OTLP_ENDPOINT.

Documentation and examples

Top-level README update
- Add a short section “Filesystem MCP Server” with purpose and quickstart.

Dedicated server docs: docs/mcp-filesystem.md (new)
- Configuration keys and defaults.
- Security notes and threat model summary.
- Quickstart with docker-compose and Aspire examples.
- Example tool calls:
  - Example .NET snippet using existing IMcpClientProvider to call filesystem.read and list.
- Resource URI examples (fs://workspace/README.md).

CI/build/release integration

- New workflow: .github/workflows/mcp-filesystem.yml
  - jobs: lint, typecheck, test (Windows + Ubuntu matrix), build, optionally publish to registry on tags.
- Add a license check step if repository uses one for JS packages; ensure dependency license policy compliance.

Gateway wiring (docker-compose and Aspire)

docker-compose option A: dynamic servers via config file
- Add a config volume with gateway.yaml that includes both servers:
  - servers:
    - name: obsidian
    - name: filesystem
      command: ["node", "/servers/filesystem-mcp/dist/index.js"]
      env:
        - FILESYSTEM_MCP__ROOTS=workspace=/workspace;home=/home/app
        - FILESYSTEM_MCP__READ_ONLY=true
- Update compose:
  - mcp-gateway service mounts servers/filesystem-mcp/dist and config; run mcp gateway run --config /etc/mcp/gateway.yaml

docker-compose option B: explicit args
- Use gateway’s --servers flag if it supports multiple registrations (subject to gateway capabilities), supplying a descriptor for filesystem.

Aspire AppHost
- Update [C#.AppHost configuration](ObsidianAI.AppHost/AppHost.cs:4) to include filesystem in --servers or switch to a config-file approach:
  - [C#.DistributedApplication.CreateBuilder()](ObsidianAI.AppHost/AppHost.cs:1) adds an additional argument to pass the filesystem server and sets env vars: FILESYSTEM_MCP__ROOTS, FILESYSTEM_MCP__READ_ONLY.

Rollout and risk mitigation

- Default read-only mode; feature flags FILESYSTEM_MCP__READ_ONLY=false to enable writes only in dev/staging first.
- Watchers disabled by default; enable behind FILESYSTEM_MCP__ENABLE_WATCHERS.
- Soft limits tuned conservatively; document overrides.
- Backwards compatibility: IMcpToolCatalog will discover new tools but existing flows unaffected unless explicitly invoked.
- Rollback: remove filesystem registration from gateway; server package remains unused.

Maintenance plan and ownership

- Code ownership: assign to platform/integration team managing MCP components.
- Keep dependencies minimal; schedule monthly dependency updates.
- Add security review for symlink/TOCTOU logic after major changes; include SAST for JS code.

Concrete interfaces and schemas (examples)

JSON Schema snippets (to be placed in handlers; example using JSON Schema draft-07)
- filesystem.list.request schema:
  - { type: "object", properties: { path: { type: "string" }, root: { type: "string" }, page: { type: "integer", minimum: 1 }, pageSize: { type: "integer", minimum: 1, maximum: 500 }, types: { type: "array", items: { enum: ["file","directory"] } }, glob: { type: "string" }, includeDotFiles: { type: "boolean" }, maxDepth: { type: "integer", minimum: 0 } }, required: ["path"] }
- filesystem.read.request schema:
  - { type: "object", properties: { path: { type: "string" }, root: { type: "string" }, encoding: { enum: ["utf-8","base64","binary"] }, maxBytes: { type: "integer", minimum: 1 }, stream: { type: "boolean" } }, required: ["path"] }

Example requests/responses
- filesystem.list request:
  - { "root": "workspace", "path": ".", "page": 1, "pageSize": 100, "glob": "**/*.md" }
- Response:
  - { "items": [{ "name": "README.md", "type": "file", "size": 1234, "mtime": "2025-10-18T12:34:56Z" }], "page": 1, "pageSize": 100, "total": 57, "truncated": false }
- filesystem.read request:
  - { "root": "workspace", "path": "docs/Technical-Overview-15-10-25.md", "encoding": "utf-8", "maxBytes": 500000 }
- Response:
  - { "encoding": "utf-8", "data": "# Overview...", "bytesRead": 4521, "truncated": false }

Starter skeleton (minimal to boot)
- servers/filesystem-mcp/package.json:
  - name: @obsidianai/filesystem-mcp, version 0.1.0, scripts: build (tsc), start (node dist/index.js), dev (tsx src/index.ts), test (vitest).
- servers/filesystem-mcp/src/index.ts:
  - Implement [TypeScript.bootstrapServer()](servers/filesystem-mcp/src/index.ts:1):
    - loadConfig(); setupTelemetry(); setupLogger();
    - instantiate MCP server via @modelcontextprotocol/sdk;
    - register tools: filesystem.version only initially via [TypeScript.registerFilesystemTools()](servers/filesystem-mcp/src/index.ts:1);
    - start the server loop; log “Filesystem MCP ready”.
- servers/filesystem-mcp/src/config.ts:
  - [TypeScript.loadConfig()](servers/filesystem-mcp/src/config.ts:1) reading FILESYSTEM_MCP__ROOTS, parsing aliases and absolute paths, validating existence at startup.

Follow-up tasks to reach full parity
- Implement remaining read-only tools (stat, normalize, list, read, search) + resources list/read.
- Add write/append/create/move/copy/delete with atomic semantics and conflict policies.
- Add watcher tools (behind flag).
- Add comprehensive tests across platforms.
- Wire gateway config and Aspire/docker-compose.
- Add telemetry and metrics, docs, and CI publish pipeline.

Cross-references to existing code for experience parity
- Client creation pattern to remain unchanged: [C#.McpClientService.GetClientAsync()](ObsidianAI.Web/Services/McpClientService.cs:31), [C#.MicrosoftLearnMcpClient.GetClientAsync()](ObsidianAI.Web/Services/MicrosoftLearnMcpClient.cs:24).
- Tool catalog integration remains as-is: [C#.McpToolCatalog.GetToolsAsync()](ObsidianAI.Application/Services/McpToolCatalog.cs:40).
- Health checks mirror: [C#.McpHealthCheck.CheckHealthAsync()](ObsidianAI.Infrastructure/HealthChecks/McpHealthCheck.cs:27), [C#.MicrosoftLearnHealthCheck.CheckHealthAsync()](ObsidianAI.Web/HealthChecks/MicrosoftLearnHealthCheck.cs:21).

Gateway integration examples

docker-compose.yml (illustrative)
- mcp-gateway:
  - command: ["mcp","gateway","run","--transport","streaming","--port","8033","--config","/etc/mcp/gateway.yaml"]
  - volumes:
    - ./servers/filesystem-mcp/dist:/servers/filesystem-mcp/dist:ro
    - ./gateway.yaml:/etc/mcp/gateway.yaml:ro
- gateway.yaml (new):
  - servers:
    - name: obsidian
    - name: filesystem
      command: ["node","/servers/filesystem-mcp/dist/index.js"]
      env:
        - FILESYSTEM_MCP__ROOTS=workspace=/workspace
        - FILESYSTEM_MCP__READ_ONLY=true

Aspire update (illustrative)
- Extend [C#.AppHost](ObsidianAI.AppHost/AppHost.cs:4) to mount filesystem server image or local node, pass FILESYSTEM_MCP__ROOTS and include filesystem in --servers via a config file.

Mermaid diagram

flowchart LR
  A[Clients in Web/API] -->|IMcpClientProvider| B(MCP Gateway)
  B -->|servers: obsidian| C[Obsidian Vault MCP Server]
  B -->|servers: filesystem| D[New FileSystem MCP Server]
  B -->|direct endpoint| E[Microsoft Learn MCP (external)]
  D -->|sandbox roots| F[(Host filesystem)]
  style D fill:#e0f7fa,stroke:#006064
  style F fill:#fff3e0,stroke:#ef6c00

Assumptions and open questions

Assumptions
- The Microsoft Learn and Obsidian servers are external; no server code in this repo. We will create a new TypeScript server inside this repo for convenience and parity of developer experience.
- Node 18+ environment is available for building/running the server in CI and via docker-compose/Aspire.
- Publishing destination: either npm (public or GH Packages) or used as source-mounted in compose.

Open questions
- Confirm preferred stack: TypeScript (@modelcontextprotocol/sdk) vs Python (mcp). This plan assumes TypeScript.
- Confirm monorepo placement and package manager (servers/filesystem-mcp; npm vs pnpm).
- Confirm gateway registration method preference (config file vs --servers flags).
- Confirm default sandbox roots (e.g., workspace to c:\git\ObsidianAI, optional additional roots).
- Confirm if watchers should be implemented in v1 or kept for later.

This plan is implementation-ready and will integrate cleanly with the existing MCP client patterns, health checks, and tool catalog, defaulting to safe read-only mode and progressively enabling write operations and watchers behind explicit feature flags.