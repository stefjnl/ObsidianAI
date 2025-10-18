namespace ObsidianAI.Domain.Models;

/// <summary>
/// Represents a provider-agnostic chat input consisting of the user's message.
/// </summary>
public sealed record ChatInput
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInput"/> class for serialization scenarios.
	/// </summary>
	public ChatInput()
	{
		Message = string.Empty;
		Attachments = new List<AttachmentContent>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInput"/> class.
	/// </summary>
	/// <param name="message">The current user message to send for processing.</param>
	/// <param name="attachments">Optional list of file attachments.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or whitespace.</exception>
	public ChatInput(string message, List<AttachmentContent>? attachments = null)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			throw new ArgumentException("Message cannot be empty.", nameof(message));
		}

		Message = message;
		Attachments = attachments ?? new List<AttachmentContent>();
	}

	/// <summary>
	/// Gets the message content supplied by the user.
	/// </summary>
	public string Message { get; init; }

	/// <summary>
	/// Gets the list of file attachments included with this message.
	/// </summary>
	public List<AttachmentContent> Attachments { get; init; }
}

/// <summary>
/// Represents the content of a file attachment.
/// </summary>
public sealed record AttachmentContent
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AttachmentContent"/> class.
	/// </summary>
	/// <param name="filename">The name of the file.</param>
	/// <param name="content">The text content of the file.</param>
	/// <param name="fileType">The file type/extension.</param>
	public AttachmentContent(string filename, string content, string fileType)
	{
		Filename = filename;
		Content = content;
		FileType = fileType;
	}

	/// <summary>
	/// Gets the filename.
	/// </summary>
	public string Filename { get; init; }

	/// <summary>
	/// Gets the text content of the file.
	/// </summary>
	public string Content { get; init; }

	/// <summary>
	/// Gets the file type/extension.
	/// </summary>
	public string FileType { get; init; }
}