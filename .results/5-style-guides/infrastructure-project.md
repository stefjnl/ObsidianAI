# infrastructure-project Style Guide

- Treat `ObsidianAI.Infrastructure.csproj` as the single integration point for external libraries (EF Core, MCP clients, AI SDKs); verify package versions match the rest of the solution.
- Preserve the project reference ordering (Domain → Application → Infrastructure) to avoid circular dependencies.
- When adding new packages, include `PrivateAssets="all"` for analyzers and restrict preview packages to explicit comment justification.
