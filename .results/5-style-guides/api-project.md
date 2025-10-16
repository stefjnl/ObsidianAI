# api-project Style Guide

- Target `net9.0` with nullable reference types and implicit usings enabled; all API code should compile under these defaults.
- Reference Microsoft Agent Framework packages alongside `Microsoft.Extensions.AI`—they power the chat agent and must stay in sync with Infrastructure.
- Keep `Microsoft.EntityFrameworkCore.Design` marked as `<PrivateAssets>all</PrivateAssets>` so tooling dependencies do not leak into consumers.
- Maintain project references to ServiceDefaults, Application, Domain, and Infrastructure; the API layer should never depend directly on the Web project.
- Treat the `.csproj.user` file as IDE state; do not commit functional settings there.
