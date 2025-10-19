using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents a chat message in the conversation
    /// </summary>
    public record ChatMessage
    {
    /// <summary>
    /// Unique identifier assigned by the server once persisted.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Client-generated identifier used for optimistic UI updates prior to persistence.
    /// </summary>
    public string ClientId { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The content of the message
        /// </summary>
        [Required(ErrorMessage = "Message content is required.")]
        [StringLength(10000, ErrorMessage = "Message cannot exceed 10,000 characters.")]
        public string Content { get; init; } = string.Empty;
        
        /// <summary>
        /// The sender of the message (User or AI)
        /// </summary>
        public MessageSender Sender { get; init; }
        
        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Optional action card data associated with this message
        /// </summary>
    public ActionCardData? ActionCard { get; init; }
        
        /// <summary>
        /// Optional file operation data associated with this message
        /// </summary>
        /// <remarks>
        /// When present, the chat UI will render a FileOperationResult component for this message.
        /// </remarks>
        public FileOperationData? FileOperation { get; init; }
        
        /// <summary>
        /// Optional search results associated with this message
        /// </summary>
    public List<SearchResultData> SearchResults { get; init; } = new();

    /// <summary>
    /// Tools invoked while generating this message.
    /// </summary>
    public List<string> ToolsUsed { get; init; } = new();

    /// <summary>
    /// Reference URLs associated with this message (for example, Microsoft Learn docs).
    /// </summary>
    public List<string> Sources { get; init; } = new();

    /// <summary>
    /// Token usage metrics reported for this message, when available.
    /// </summary>
    public TokenUsageSummary? Usage { get; init; }
        
        /// <summary>
        /// Whether the message is currently being processed
        /// </summary>
        public bool IsProcessing { get; init; }
        
        /// <summary>
        /// Type of processing being performed
        /// </summary>
        public ProcessingType ProcessingType { get; init; }

        /// <summary>
        /// Indicates the message is awaiting server confirmation.
        /// </summary>
        public bool IsPending { get; init; }
    }

    /// <summary>
    /// Enum for message senders
    /// </summary>
    public enum MessageSender
    {
        User,
        AI
    }

    /// <summary>
    /// Enum for processing types
    /// </summary>
    public enum ProcessingType
    {
        None,
        Searching,
        Writing,
        Reorganizing,
        Thinking
    }
}