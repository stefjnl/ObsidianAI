namespace ObsidianAI.Application.DTOs;

public record GenerateContentRequest(
    string Prompt,
    string? Context = null,
    string? Provider = null,
    string? Model = null);