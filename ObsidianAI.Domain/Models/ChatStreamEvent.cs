namespace ObsidianAI.Domain.Models
{
    /// <summary>
    /// Represents an event emitted during chat streaming, which can be either a text chunk or a tool call marker.
    /// </summary>
    public enum ChatStreamEventKind
    {
        /// <summary>
        /// Indicates a chunk of text output from the model.
        /// </summary>
        Text,

        /// <summary>
        /// Indicates a tool call marker emitted by the model.
        /// </summary>
        ToolCall
    }

    /// <summary>
    /// Strongly-typed event representing streaming output from a chat agent.
    /// </summary>
    /// <param name="Kind">Kind of event: text or tool call.</param>
    /// <param name="Text">Optional text content when <see cref="ChatStreamEventKind.Text"/>.</param>
    /// <param name="ToolName">Optional tool name when <see cref="ChatStreamEventKind.ToolCall"/>.</param>
    public sealed record ChatStreamEvent(ChatStreamEventKind Kind, string? Text = null, string? ToolName = null)
    {
        /// <summary>
        /// Creates a text chunk event.
        /// </summary>
        /// <param name="text">The text payload of the chunk.</param>
        /// <returns>A <see cref="ChatStreamEvent"/> with kind <see cref="ChatStreamEventKind.Text"/>.</returns>
        public static ChatStreamEvent TextChunk(string text) => new(ChatStreamEventKind.Text, text);

        /// <summary>
        /// Creates a tool call event.
        /// </summary>
        /// <param name="toolName">The name of the tool being invoked.</param>
        /// <returns>A <see cref="ChatStreamEvent"/> with kind <see cref="ChatStreamEventKind.ToolCall"/>.</returns>
        public static ChatStreamEvent ToolCall(string toolName) => new(ChatStreamEventKind.ToolCall, ToolName: toolName);
    }
}