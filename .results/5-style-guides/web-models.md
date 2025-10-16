# web-models Style Guide

- Web models mirror API payloads but can include UI-only helpers; keep naming consistent with API models to reduce mapping friction.
- Make properties nullable only when the UI can render missing data; prefer non-nullable defaults for required fields.
- When adding new models, implement mapper extensions or constructors to translate from application DTOs in a single place.
