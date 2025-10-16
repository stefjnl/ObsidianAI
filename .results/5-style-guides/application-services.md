# application-services Style Guide

- Prefer dedicated service classes (e.g., `RegexFileOperationExtractor`, `VaultPathResolver`) to keep parsing and resolution logic reusable across use cases.
- Path normalization is handled by `BasicVaultPathNormalizer` plus `PathNormalizer`; reuse those abstractions instead of manipulating paths manually.
- `VaultPathResolver` combines MCP listing results with normalizer match keys; when adding new resolution strategies, plug them into this resolver rather than branching in use cases.
- Keep regex patterns for file operation extraction in a single array so new keywords can be appended without altering control flow.
