# infrastructure-vault Style Guide

- Implement `IVaultToolExecutor` adapters here; ensure operations are idempotent and log vault mutations for auditability.
- Provide a `Null` executor for scenarios where the vault is disabled; guard public methods with `ArgumentNullException.ThrowIfNull` to maintain contract safety.
- Keep MCP invocation details isolated so higher layers remain agnostic to the protocol.
