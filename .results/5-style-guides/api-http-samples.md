# api-http-samples Style Guide

- Keep REST client samples scoped to host variables (e.g., `@ObsidianAI.Api_HostAddress`) so developers can toggle between local ports without editing individual requests.
- The `.http` file should illustrate canonical endpoints; replace the placeholder weather sample with real chat or conversation routes when expanding coverage.
- Use `Accept: application/json` headers explicitly to mirror what the Blazor client requests.
