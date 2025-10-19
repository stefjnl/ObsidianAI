using Microsoft.Extensions.AI;

namespace ObsidianAI.Domain.Models
{
    /// <summary>
    /// Represents the result of a non-streaming chat interaction.
    /// </summary>
    /// <param name="Text">Assistant response text.</param>
    /// <param name="Usage">Optional token usage details returned by the provider.</param>
    public sealed record ChatResponse(string Text, UsageDetails? Usage = null);
}
