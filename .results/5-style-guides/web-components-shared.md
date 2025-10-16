# web-components-shared Style Guide

- Shared components should be presentational; accept strongly typed parameters and emit events rather than calling services directly.
- Co-locate component-specific styles in `.razor.css` files and scope them to avoid leaking across the app.
- Ensure reusable components support loading and empty states so parent pages can render gracefully.
