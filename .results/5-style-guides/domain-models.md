# domain-models Style Guide

- Domain models (records) encapsulate value semantics and validation; e.g., `ChatInput` throws when constructed with whitespace, ensuring upstream layers sanitize input.
- Provide parameterless constructors only when serialization frameworks require them—document why they exist.
- Favor immutable init-only properties to keep domain state predictable once constructed.
