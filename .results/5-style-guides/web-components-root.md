# web-components-root Style Guide

- `App.razor` and `_Imports.razor` should stay lean; configure global namespaces and root-level layout routing only.
- Keep `_Imports.razor` limited to namespaces/components used across multiple files to avoid unnecessary global usings.
