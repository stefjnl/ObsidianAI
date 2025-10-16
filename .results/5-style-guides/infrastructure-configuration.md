# infrastructure-configuration Style Guide

- Mirror the structure of `appsettings.*.json`; configuration POCOs should use nullable reference types and accurate property names for binding.
- Provide sensible defaults in parameterless constructors to keep local development simple while still supporting overrides.
- Validate critical settings (API keys, base URLs) during startup in Infrastructure DI extensions to fail fast when misconfigured.
