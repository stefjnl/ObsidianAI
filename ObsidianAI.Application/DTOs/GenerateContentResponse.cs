namespace ObsidianAI.Application.DTOs;

public record GenerateContentResponse(
    string Content,
    string Provider,
    string Model,
    int TokensUsed);