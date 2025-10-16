# application-project Style Guide

- Target `net9.0` with `TreatWarningsAsErrors` enabled—new code must compile clean with nullable and analyzer warnings resolved.
- Reference only the Domain project; infrastructure dependencies belong elsewhere to preserve Clean Architecture boundaries.
- Include `ModelContextProtocol` when application services need to pass MCP types; keep versioning in sync with infrastructure to avoid mismatches.
