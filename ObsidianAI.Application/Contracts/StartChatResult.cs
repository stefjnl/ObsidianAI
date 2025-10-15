namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Represents the result of starting a chat, including the response text and any extracted file operation.
/// </summary>
/// <param name="Text">The full text response from the AI agent.</param>
/// <param name="FileOperation">An optional file operation extracted from the response.</param>
public sealed record StartChatResult(string Text, ObsidianAI.Domain.Models.FileOperation? FileOperation);