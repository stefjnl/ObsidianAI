# solution-config Style Guide

- Mirror the existing `docker-compose.yml` pattern: API and web services share the root build context and reference their project-specific Dockerfiles while exposing ports `5095` and `5244` respectively.
- Pass connection strings and LLM configuration via `LLM__*` environment variables; avoid hardcoding provider values in compose files so local `.env` overrides continue to work.
- Persist the SQLite database by binding `/app/data/obsidianai.db` to the named volume `obsidianai-data`; new services that read the database should mount the same volume.
- Keep the MCP gateway defined as a dedicated service using the upstream `ghcr.io/modelcontextprotocol/gateway` image with the existing `mcp gateway run` command arguments.
