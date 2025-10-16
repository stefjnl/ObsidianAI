# web-appsettings Style Guide

- Keep Web app settings focused on UI concerns (SignalR endpoints, feature flags) and defer shared settings to ServiceDefaults.
- Mirror keys between `appsettings.json` and `appsettings.Development.json`; document overrides with inline comments if values diverge.
- Avoid embedding secrets; rely on user secrets or environment variables for API keys.
