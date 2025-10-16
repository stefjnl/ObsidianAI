# web-scripts Style Guide

- Keep JavaScript minimal and scoped to behaviors Blazor cannot yet provide (e.g., streaming text decoding).
- Wrap interop functions in `window.ObsidianAI` namespaces to avoid polluting global scope.
- Document dependencies (SignalR, DOM APIs) and ensure scripts are registered in `_Host.cshtml` or equivalent when new functions are added.
