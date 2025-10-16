# aspire-config Style Guide

- Use AppHost `appsettings*.json` to tune orchestration-level logging only; runtime configuration for API/Web belongs in their respective projects.
- Override the `Aspire.Hosting.Dcp` log level to `Warning` in production settings to suppress noisy diagnostics while keeping default info logs.
- Keep development and default files minimal—any additional configuration should be in code via the Distributed Application builder.
