# web-components-layout Style Guide

- Layout components manage chrome and navigation; keep CSS co-located (`.razor.css`) and scoped via `@layer` when necessary.
- Use cascading parameters to broadcast theme or context info rather than injecting services into every child component.
- Document changes to navigation structure in `NavMenu.razor` comments when adding new top-level pages.
