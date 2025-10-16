# api-service-contracts Style Guide

- `Services/ILlmClientFactory.cs` is a namespace shim that aliases the infrastructure interface into the API project; keep it as a global using rather than duplicating the interface.
- When moving contracts, prefer shims like this to avoid mass refactors—mark them with comments indicating the canonical namespace.
