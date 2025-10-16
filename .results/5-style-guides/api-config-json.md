# api-config-json Style Guide

- Keep the SQLite connection string pointing to `obsidianai.db` in the project root; Docker overrides remap it to `/app/data/obsidianai.db` via environment variables.
- Default the `LLM.Provider` to `OpenRouter` and include both provider blocks (`LMStudio`, `OpenRouter`) even if one is unused—runtime selection depends on these nested settings.
- Use `appsettings.Development.json` exclusively for logging verbosity adjustments; do not duplicate LLM configuration there to avoid drift.
- Expose overrides through hierarchical environment variables (`LLM__OpenRouter__Model`, etc.) so container deployments can swap providers without editing the JSON files.
