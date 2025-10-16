# api-configuration Style Guide

- Keep agent instructions centralized in `AgentInstructions.ObsidianAssistant`; when updating prompt text, preserve the emoji normalization workflow and the confirmation rules for write/delete operations.
- Register services through extension methods (`ServiceRegistration.AddObsidianApiServices`) so `Program.cs` stays declarative; new services should be appended there with clear lifetimes.
- Define routes exclusively inside `EndpointRegistration.MapObsidianEndpoints`; follow the existing pattern of binding request DTOs, invoking use cases, and projecting anonymous objects.
- When adding new endpoints, provide minimal result shapes (anonymous objects) rather than returning raw entities to avoid leaking database schemas.
