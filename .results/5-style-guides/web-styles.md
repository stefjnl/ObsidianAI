# web-styles Style Guide

- Keep global styles in `wwwroot/app.css` and chat-specific rules in `wwwroot/css/chat.css`; document selectors when they pair with component parameters.
- Prefer CSS variables for theme values to simplify dark/light mode toggles.
- Ensure class names follow BEM-style semantics (`chat-area__header`) to avoid collisions.
