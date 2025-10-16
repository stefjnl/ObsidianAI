# web-components-pages Style Guide

- Page components own routing and orchestrate child components; keep them thin by delegating business logic to services.
- Use `OnInitializedAsync` for async data loading and guard against race conditions with cancellation tokens when navigating rapidly.
- When adding new pages, update route definitions and ensure navigation items point to the same URLs.
