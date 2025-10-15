namespace ObsidianAI.Domain.Models;

/// <summary>
/// Represents a provider-agnostic chat input consisting of the user's message and optional prior conversation history.
/// </summary>
public sealed record ChatInput
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInput"/> class for serialization scenarios.
	/// </summary>
	public ChatInput()
	{
		Message = string.Empty;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInput"/> class.
	/// </summary>
	/// <param name="message">The current user message to send for processing.</param>
	/// <param name="history">An optional chronological list of prior conversation messages to provide context.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or whitespace.</exception>
	public ChatInput(string message, System.Collections.Generic.IReadOnlyList<ConversationMessage>? history = null)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			throw new ArgumentException("Message cannot be empty.", nameof(message));
		}

		Message = message;
		History = history;
	}

	/// <summary>
	/// Gets the message content supplied by the user.
	/// </summary>
	public string Message { get; init; }

	/// <summary>
	/// Gets the optional conversation history that provides context for the request.
	/// </summary>
	public System.Collections.Generic.IReadOnlyList<ConversationMessage>? History { get; init; }
}