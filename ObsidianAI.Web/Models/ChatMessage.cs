using System;

namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents a chat message in the conversation
    /// </summary>
    public record ChatMessage
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The content of the message
        /// </summary>
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
        /// Whether the message is currently being processed
        /// </summary>
        public bool IsProcessing { get; init; }
        
        /// <summary>
        /// Type of processing being performed
        /// </summary>
        public ProcessingType ProcessingType { get; init; }
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