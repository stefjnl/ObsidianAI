namespace ObsidianAI.Domain.Models;

/// <summary>
/// Represents a provider-agnostic chat input consisting of the user's message and optional prior conversation history.
/// </summary>
/// <param name="Message">The current user message to send for processing.</param>
/// <param name="History">An optional chronological list of prior conversation messages to provide context; may be null or empty.</param>
public sealed record ChatInput(string Message, System.Collections.Generic.IReadOnlyList<ConversationMessage>? History = null);