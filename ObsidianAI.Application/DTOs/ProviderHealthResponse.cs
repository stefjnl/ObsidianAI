namespace ObsidianAI.Application.DTOs;

public record ProviderHealthResponse(
    string ProviderName,
    bool IsHealthy);