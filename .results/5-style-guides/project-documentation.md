# project-documentation Style Guide

- Documentation favors architecture-first storytelling: mirror `README.md` by opening with a high-level product description, then layering feature highlights, architecture diagrams, and onboarding steps.
- When adding docs under `docs/`, follow the existing pattern of scenario-specific Markdown files (`conversation-history.md`, `Obsidian-Chat-Interface.md`) or include single-page HTML prototypes like `obsidian-ai-ui-light.html` when visuals matter.
- Cross-link code artifacts directly using relative paths (e.g., ``[`ObsidianAssistantService.cs`](ObsidianAI.Api/Services/ObsidianAssistantService.cs)``) to help new contributors jump into the code.
- Preserve the date-stamped naming convention (`Technical-Overview-15-10-25.md`) for temporal design snapshots so readers can track architecture evolution.
