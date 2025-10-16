# root-environment Style Guide

- Keep `.dockerignore` aligned with build output expectations: it currently excludes solution-level Markdown, `.env`, compiled binaries, and Python caches so multi-stage Docker builds stay lean.
- Populate `.env.example` with the same set of `LLM__*` keys consumed in `docker-compose.yml`; do not add app-specific secrets, only placeholders or local defaults.
- Maintain `.gitignore` entries that omit `appsettings*.json` and other environment-specific configs—commit new configuration templates under `*.example` instead of real secrets.
- Never commit a populated `.env`; the runtime reads overrides from the developer’s local file that mirrors `.env.example`.
